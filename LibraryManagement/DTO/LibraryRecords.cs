namespace LibraryManagement.DTO;

internal sealed record ReaderRecord(string Name, string Email, DateTime ExpiresAt);
internal sealed record BookRecord(string Title, string Genre, int Available);
internal sealed record LoanLine(int BookId, int Quantity);
