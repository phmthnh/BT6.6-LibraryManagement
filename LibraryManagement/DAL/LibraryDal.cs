using System.Data;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL;

internal sealed class LibraryDal
{
    internal DataTable Readers() => Db.Query("""
        SELECT MaDocGia, HoTen, Email, NgayHetHanThe FROM DocGia ORDER BY HoTen, MaDocGia
        """);

    internal DataTable Books() => Db.Query("""
        SELECT MaSach, TenSach, TheLoai, SoLuongCon FROM DauSach ORDER BY TenSach, MaSach
        """);

    internal void SaveReader(int? id, ReaderRecord reader) => Db.Execute(id is null
        ? "INSERT DocGia(HoTen,Email,NgayHetHanThe) VALUES(@name,@email,@expires)"
        : "UPDATE DocGia SET HoTen=@name, Email=@email, NgayHetHanThe=@expires WHERE MaDocGia=@id",
        ("@name", reader.Name), ("@email", string.IsNullOrWhiteSpace(reader.Email) ? null : reader.Email), ("@expires", reader.ExpiresAt), ("@id", id));

    internal void SaveBook(int? id, BookRecord book) => Db.Execute(id is null
        ? "INSERT DauSach(TenSach,TheLoai,SoLuongCon) VALUES(@title,@genre,@stock)"
        : "UPDATE DauSach SET TenSach=@title, TheLoai=@genre, SoLuongCon=@stock WHERE MaSach=@id",
        ("@title", book.Title), ("@genre", book.Genre), ("@stock", book.Available), ("@id", id));

    internal void DeleteReader(int id) => Db.Execute("DELETE DocGia WHERE MaDocGia=@id", ("@id", id));
    internal void DeleteBook(int id) => Db.Execute("DELETE DauSach WHERE MaSach=@id", ("@id", id));

    internal DataTable Loans(int? reader, string? state) => Db.Query("""
        SELECT p.MaPhieu, p.MaDocGia, d.HoTen, p.NgayMuon, p.HanTra, s.TinhTrang AS TrangThai
        FROM PhieuMuon p JOIN DocGia d ON d.MaDocGia=p.MaDocGia
        CROSS APPLY (VALUES (CASE WHEN p.TrangThai<>N'Đã trả' AND p.HanTra<GETDATE()
            THEN N'Quá hạn' ELSE p.TrangThai END)) s(TinhTrang)
        WHERE (@reader IS NULL OR p.MaDocGia=@reader) AND (@state IS NULL OR s.TinhTrang=@state)
        ORDER BY p.MaPhieu DESC
        """, ("@reader", reader), ("@state", state));

    internal DataTable Details(int loan) => Db.Query("""
        SELECT b.MaSach,b.TenSach,b.TheLoai,l.SoLuong
        FROM ChiTietMuon l JOIN DauSach b ON b.MaSach=l.MaSach WHERE l.MaPhieu=@id
        ORDER BY b.TenSach
        """, ("@id", loan));

    internal DataTable Overdue() => Db.Query("""
        SELECT p.MaPhieu, d.HoTen, p.NgayMuon, p.HanTra
        FROM PhieuMuon p JOIN DocGia d ON d.MaDocGia=p.MaDocGia
        WHERE p.HanTra < GETDATE() AND p.TrangThai <> N'Đã trả' ORDER BY p.HanTra, p.MaPhieu
        """);

    internal DataTable ByGenre() => Db.Query("""
        SELECT b.TheLoai, COUNT(*) AS SoLuotMuon
        FROM ChiTietMuon l JOIN DauSach b ON b.MaSach=l.MaSach
        GROUP BY b.TheLoai ORDER BY SoLuotMuon DESC,b.TheLoai
        """);

    internal int Borrow(int reader, DateTime due, IReadOnlyList<LoanLine> lines) =>
        Db.InTransaction((connection, transaction) =>
        {
            using var card = Db.Command(connection, transaction, """
                SELECT CASE WHEN NgayHetHanThe >= GETDATE() THEN 1 ELSE 0 END
                FROM DocGia WITH (UPDLOCK, HOLDLOCK) WHERE MaDocGia=@id
                """, ("@id", reader));
            if (card.ExecuteScalar() is not 1)
                throw new InvalidOperationException("Thẻ độc giả đã hết hạn hoặc độc giả không tồn tại.");

            using var loan = Db.Command(connection, transaction, """
                DECLARE @now datetime=GETDATE();
                IF @due > @now
                    INSERT PhieuMuon(MaDocGia,NgayMuon,HanTra,TrangThai)
                    OUTPUT inserted.MaPhieu VALUES(@reader,@now,@due,N'Đang mượn');
                """, ("@reader", reader), ("@due", due));
            if (loan.ExecuteScalar() is not int loanId)
                throw new ArgumentException("Hạn trả phải sau thời điểm lập phiếu.");

            foreach (var line in lines.OrderBy(line => line.BookId))
            {
                using var stock = Db.Command(connection, transaction, """
                    UPDATE DauSach SET SoLuongCon=SoLuongCon-@qty WHERE MaSach=@book AND SoLuongCon>=@qty
                    """, ("@book", line.BookId), ("@qty", line.Quantity));
                if (stock.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException($"Sách mã {line.BookId} không đủ tồn. Phiếu mượn đã được hoàn tác.");

                using var detail = Db.Command(connection, transaction, """
                    INSERT ChiTietMuon(MaPhieu,MaSach,SoLuong) VALUES(@loan,@book,@qty)
                    """, ("@loan", loanId), ("@book", line.BookId), ("@qty", line.Quantity));
                detail.ExecuteNonQuery();
            }
            return loanId;
        });

    internal void Return(int loanId) => Db.InTransaction((connection, transaction) =>
    {
        using var change = Db.Command(connection, transaction, """
            UPDATE PhieuMuon SET TrangThai=N'Đã trả'
            WHERE MaPhieu=@id AND TrangThai IN (N'Đang mượn',N'Quá hạn')
            """, ("@id", loanId));
        if (change.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Phiếu đã được trả hoặc không còn tồn tại.");

        using var stock = Db.Command(connection, transaction, """
            UPDATE b SET b.SoLuongCon=b.SoLuongCon+l.SoLuong
            FROM DauSach b JOIN ChiTietMuon l ON l.MaSach=b.MaSach WHERE l.MaPhieu=@id
            """, ("@id", loanId));
        stock.ExecuteNonQuery();
        return true;
    });
}
