using System.Data;
using LibraryManagement.DAL;
using LibraryManagement.DTO;

namespace LibraryManagement.BUS;

internal sealed class LibraryBus
{
    private readonly LibraryDal data = new();

    internal DataTable Readers() => data.Readers();
    internal DataTable Books() => data.Books();
    internal DataTable Loans(int? reader, string? state) => data.Loans(reader, state);
    internal DataTable Details(int loan) => data.Details(loan);
    internal DataTable Overdue() => data.Overdue();
    internal DataTable ByGenre() => data.ByGenre();

    internal void SaveReader(int? id, ReaderRecord reader)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reader.Name, "Họ tên");
        data.SaveReader(id, reader);
    }

    internal void SaveBook(int? id, BookRecord book)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(book.Title, "Tên sách");
        if (book.Available < 0)
            throw new ArgumentException("Số lượng còn phải từ 0 trở lên.");
        data.SaveBook(id, book);
    }

    internal void DeleteReader(int id) => data.DeleteReader(id);
    internal void DeleteBook(int id) => data.DeleteBook(id);

    internal int Borrow(int reader, DateTime due, IReadOnlyList<LoanLine> lines)
    {
        if (lines.Count == 0 || lines.Any(line => line.Quantity <= 0)
            || lines.Select(line => line.BookId).Distinct().Count() != lines.Count)
            throw new ArgumentException("Thêm ít nhất một đầu sách với số lượng dương và mỗi đầu sách một dòng.");
        return data.Borrow(reader, due, lines);
    }

    internal void Return(int loan) => data.Return(loan);
}
