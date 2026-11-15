using System.Data;
using LibraryManagement.BUS;
using LibraryManagement.DTO;

namespace LibraryManagement.GUI;

public partial class MainForm : Form
{
    private readonly LibraryBus service = new();
    private readonly DataTable draft = new();
    private int? editingBook;
    private int? editingReader;

    public MainForm()
    {
        InitializeComponent();
        Shown += (_, _) => Ui.Run(this, InitializeData);
    }

    private void InitializeData()
    {
        draft.Columns.Add("MaSach", typeof(int));
        draft.Columns.Add("TenSach", typeof(string));
        draft.Columns.Add("SoLuong", typeof(int));
        draft.PrimaryKey = [draft.Columns["MaSach"]!];
        BindGrid(draftGrid, draft);
        due.Value = DateTime.Now.AddDays(7);
        statusFilter.Items.AddRange(["Tất cả", "Đang mượn", "Đã trả", "Quá hạn"]);
        statusFilter.SelectedIndex = 0;
        ReloadReaders();
        ReloadBooks();
        ReloadLoans();
        ReloadReports();
    }

    private void ReloadReaders()
    {
        var items = service.Readers();
        BindGrid(readers, items);
        Ui.Bind(loanReader, items.Copy(), "HoTen", "MaDocGia");
        var filterItems = items.Copy();
        var all = filterItems.NewRow();
        all["MaDocGia"] = 0;
        all["HoTen"] = "Tất cả độc giả";
        filterItems.Rows.InsertAt(all, 0);
        Ui.Bind(readerFilter, filterItems, "HoTen", "MaDocGia");
    }

    private void ReloadBooks()
    {
        var items = service.Books();
        BindGrid(books, items);
        Ui.Bind(loanBook, items.Copy(), "TenSach", "MaSach");
    }

    private void ReloadLoans()
    {
        int? reader = readerFilter.SelectedValue is int id && id > 0 ? id : null;
        string? state = statusFilter.SelectedIndex > 0 ? (string?)statusFilter.SelectedItem : null;
        BindGrid(loans, service.Loans(reader, state));
        ReloadLoanDetails();
    }

    private void ReloadLoanDetails() =>
        BindGrid(loanDetails, Ui.Row(loans)?["MaPhieu"] is int loan ? service.Details(loan) : null);

    private void ReloadReports()
    {
        BindGrid(overdue, service.Overdue());
        BindGrid(report, service.ByGenre());
    }

    private void filterClick(object? sender, EventArgs e) => Ui.Run(this, ReloadLoans);
    private void LoanSelected(object? sender, DataGridViewCellEventArgs e) => Ui.Run(this, ReloadLoanDetails);
    private void refreshReportClick(object? sender, EventArgs e) => Ui.Run(this, ReloadReports);

    private void returnLoanClick(object? sender, EventArgs e) => Ui.Run(this, () =>
    {
        service.Return(Ui.Id(loans, "MaPhieu"));
        ReloadLoans();
        ReloadBooks();
        ReloadReports();
    });

    private void addLineClick(object? sender, EventArgs e) => Ui.Run(this, () =>
    {
        int id = Ui.Id(loanBook);
        int count = decimal.ToInt32(loanQuantity.Value);
        if (draft.Rows.Find(id) is DataRow existing)
            existing["SoLuong"] = checked((int)existing["SoLuong"] + count);
        else
            draft.Rows.Add(id, loanBook.Text, count);
        borrowStatus.Text = "";
    });

    private void removeLineClick(object? sender, EventArgs e)
    {
        if (Ui.Row(draftGrid) is DataRowView row)
            draft.Rows.Remove(row.Row);
    }

    private void saveLoanClick(object? sender, EventArgs e) => Ui.Run(this, () =>
    {
        var lines = draft.Rows.Cast<DataRow>().Select(row => new LoanLine((int)row[0], (int)row[2])).ToArray();
        int loanId = service.Borrow(Ui.Id(loanReader), due.Value, lines);
        draft.Clear();
        borrowStatus.Text = $"Đã lập phiếu mượn #{loanId}.";
        ReloadLoans();
        ReloadBooks();
        ReloadReports();
    });

    private void BookSelected(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || Ui.Row(books) is not DataRowView row)
            return;
        editingBook = (int)row["MaSach"];
        bookTitle.Text = Convert.ToString(row["TenSach"]);
        genre.Text = Convert.ToString(row["TheLoai"]);
        available.Value = row["SoLuongCon"] is int quantity ? quantity : 0;
    }

    private void newBookClick(object? sender, EventArgs e)
    {
        editingBook = null;
        bookTitle.Clear();
        genre.Clear();
        available.Value = 0;
    }

    private void saveBookClick(object? sender, EventArgs e) => Ui.Run(this, () =>
    {
        service.SaveBook(editingBook, new BookRecord(bookTitle.Text.Trim(), genre.Text.Trim(), decimal.ToInt32(available.Value)));
        ReloadBooks();
        ReloadReports();
        newBookClick(null, EventArgs.Empty);
    });

    private void deleteBookClick(object? sender, EventArgs e) => Ui.Run(this, () =>
    {
        int id = Ui.Id(books, "MaSach");
        if (MessageBox.Show(this, "Xóa đầu sách đã chọn?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;
        service.DeleteBook(id);
        ReloadBooks();
        newBookClick(null, EventArgs.Empty);
    });

    private void ReaderSelected(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || Ui.Row(readers) is not DataRowView row)
            return;
        editingReader = (int)row["MaDocGia"];
        readerName.Text = Convert.ToString(row["HoTen"]);
        email.Text = Convert.ToString(row["Email"]);
        expires.Value = row["NgayHetHanThe"] is DateTime date ? date : DateTime.Now;
    }

    private void newReaderClick(object? sender, EventArgs e)
    {
        editingReader = null;
        readerName.Clear();
        email.Clear();
        expires.Value = DateTime.Now;
    }

    private void saveReaderClick(object? sender, EventArgs e) => Ui.Run(this, () =>
    {
        service.SaveReader(editingReader, new ReaderRecord(readerName.Text.Trim(), email.Text.Trim(), expires.Value));
        ReloadReaders();
        ReloadLoans();
        newReaderClick(null, EventArgs.Empty);
    });

    private void deleteReaderClick(object? sender, EventArgs e) => Ui.Run(this, () =>
    {
        int id = Ui.Id(readers, "MaDocGia");
        if (MessageBox.Show(this, "Xóa độc giả đã chọn?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;
        service.DeleteReader(id);
        ReloadReaders();
        ReloadLoans();
        newReaderClick(null, EventArgs.Empty);
    });

    private static readonly Dictionary<string, string> Captions = new()
    {
        ["MaPhieu"]="Mã phiếu", ["MaDocGia"]="Mã độc giả", ["HoTen"]="Họ tên",
        ["NgayMuon"]="Ngày mượn", ["HanTra"]="Hạn trả", ["TrangThai"]="Trạng thái",
        ["MaSach"]="Mã sách", ["TenSach"]="Tên sách", ["TheLoai"]="Thể loại",
        ["SoLuongCon"]="Số lượng còn", ["SoLuong"]="Số lượng", ["NgayHetHanThe"]="Hạn thẻ",
        ["SoLuotMuon"]="Số lượt mượn"
    };

    private static void BindGrid(DataGridView grid, DataTable? table)
    {
        grid.DataSource = table;
        foreach (DataGridViewColumn column in grid.Columns)
            if (Captions.TryGetValue(column.DataPropertyName, out string? label))
                column.HeaderText = label;
    }
}
