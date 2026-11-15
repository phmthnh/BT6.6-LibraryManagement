IF DB_ID(N'LibraryDB') IS NULL
    EXEC(N'CREATE DATABASE [LibraryDB]');
GO
USE [LibraryDB];
GO
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.DocGia', N'U') IS NULL
BEGIN
    BEGIN TRANSACTION;

    CREATE TABLE DocGia (MaDocGia int IDENTITY PRIMARY KEY, HoTen nvarchar(100) NOT NULL,
        Email varchar(100) UNIQUE, NgayHetHanThe datetime);
    CREATE TABLE DauSach (MaSach int IDENTITY PRIMARY KEY, TenSach nvarchar(200) NOT NULL,
        TheLoai nvarchar(50), SoLuongCon int CHECK (SoLuongCon >= 0));
    CREATE TABLE PhieuMuon (MaPhieu int IDENTITY PRIMARY KEY, MaDocGia int REFERENCES DocGia,
        NgayMuon datetime DEFAULT GETDATE(), HanTra datetime, TrangThai nvarchar(50));
    CREATE TABLE ChiTietMuon (MaPhieu int REFERENCES PhieuMuon, MaSach int REFERENCES DauSach,
        SoLuong int CHECK (SoLuong > 0), PRIMARY KEY (MaPhieu, MaSach));

    INSERT DocGia(HoTen,Email,NgayHetHanThe) VALUES
    (N'Nguyễn An','an@example.test',DATEADD(day,90,GETDATE())),
    (N'Trần Bình','binh@example.test',DATEADD(day,60,GETDATE())),
    (N'Lê Minh','minh@example.test',DATEADD(day,-1,GETDATE()));
    INSERT DauSach(TenSach,TheLoai,SoLuongCon) VALUES
    (N'Lập trình C#',N'Công nghệ thông tin',8),(N'Kinh tế học',N'Kinh tế',5),
    (N'Dế Mèn phiêu lưu ký',N'Văn học',7),(N'Cơ sở dữ liệu',N'Công nghệ thông tin',3);
    INSERT PhieuMuon(MaDocGia,NgayMuon,HanTra,TrangThai)
    VALUES (2,DATEADD(day,-9,GETDATE()),DATEADD(day,-1,GETDATE()),N'Đang mượn');
    INSERT ChiTietMuon(MaPhieu,MaSach,SoLuong) VALUES (1,3,1);

    COMMIT;
END;
GO
