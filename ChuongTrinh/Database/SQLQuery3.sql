-- ====================================================================
-- CƠ SỞ DỮ LIỆU HỆ THỐNG QUẢN LÝ GIAO DỊCH NGÂN HÀNG - NHÓM 07
-- File SQL gộp duy nhất: Tạo Database, 19 bảng và dữ liệu mẫu.
-- Cách dùng trên máy mới: mở bằng SQL Server Management Studio (SSMS)
-- và nhấn Execute (F5). Có thể chạy lại nhiều lần không bị lỗi.
-- ====================================================================

IF DB_ID(N'QuanLyGiaoDichNganHang') IS NULL
BEGIN
    CREATE DATABASE QuanLyGiaoDichNganHang;
END
GO

USE QuanLyGiaoDichNganHang;
GO

-- ====================================================================
-- PHẦN 1: TẠO CẤU TRÚC 19 BẢNG
-- ====================================================================

-- 1. Bảng AccountBank (Tài khoản ngân hàng của khách hàng)
IF OBJECT_ID(N'dbo.AccountBank', N'U') IS NULL
BEGIN
    CREATE TABLE [AccountBank] (
        ID VARCHAR(13) PRIMARY KEY CHECK (LEN(ID)=13 AND ID NOT LIKE '%[^0-9]%'),
        password VARCHAR(15) NOT NULL,
        fullname NVARCHAR(300) NOT NULL,
        gender NVARCHAR(3) CHECK (gender IN (N'Nam', N'Nữ')) NOT NULL,
        birthday DATE NOT NULL,
        CCCD VARCHAR(12) CHECK (LEN(CCCD)=12 AND CCCD NOT LIKE '%[^0-9]%') NOT NULL,
        phone VARCHAR(10) CHECK (LEN(phone)=10 AND phone NOT LIKE '%[^0-9]%') NOT NULL,
        address NVARCHAR(150) NOT NULL,
        email VARCHAR(256),
        type NVARCHAR(20) CHECK (type IN (N'Tài khoản thanh toán', N'Tài khoản tiết kiệm', N'Tài khoản vay')) NOT NULL,
        providedate DATETIME NOT NULL,
        money INT CHECK (money>=50000) NOT NULL
    );
END
GO

-- 2. Bảng Role (Vai trò hệ thống)
IF OBJECT_ID(N'dbo.Role', N'U') IS NULL
BEGIN
    CREATE TABLE [Role] (
        roleId INT PRIMARY KEY,
        roleName NVARCHAR(50),
        description NVARCHAR(255)
    );
END
GO

-- 3. Bảng Permission (Quyền hạn)
IF OBJECT_ID(N'dbo.Permission', N'U') IS NULL
BEGIN
    CREATE TABLE [Permission] (
        permissionId INT PRIMARY KEY,
        permissionName NVARCHAR(100),
        description NVARCHAR(255)
    );
END
GO

-- 4. Bảng RolePermission (Bảng trung gian Role - Permission)
IF OBJECT_ID(N'dbo.RolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE [RolePermission] (
        roleId INT,
        permissionId INT,
        PRIMARY KEY (roleId, permissionId),
        FOREIGN KEY (roleId) REFERENCES [Role](roleId),
        FOREIGN KEY (permissionId) REFERENCES [Permission](permissionId)
    );
END
GO

-- 5. Bảng Account (Tài khoản nhân viên/quản trị viên)
IF OBJECT_ID(N'dbo.Account', N'U') IS NULL
BEGIN
    CREATE TABLE [Account] (
        accountId INT PRIMARY KEY,
        roleId INT,
        ID VARCHAR(13) CHECK (LEN(ID)=13 AND ID NOT LIKE '%[^0-9]%'),
        fullName NVARCHAR(100),
        username NVARCHAR(50),
        password NVARCHAR(255),
        email NVARCHAR(100),
        phoneNumber NVARCHAR(20),
        status NVARCHAR(50),
        createdDate DATETIME,
        FOREIGN KEY (roleId) REFERENCES [Role](roleId),
        FOREIGN KEY (ID) REFERENCES [AccountBank](ID)
    );
END
GO

-- 6. Bảng ActivityLog (Nhật ký hoạt động hệ thống)
IF OBJECT_ID(N'dbo.ActivityLog', N'U') IS NULL
BEGIN
    CREATE TABLE [ActivityLog] (
        logId BIGINT PRIMARY KEY,
        accountId INT,
        action NVARCHAR(100),
        timestamp DATETIME,
        result NVARCHAR(50),
        details NVARCHAR(MAX),
        FOREIGN KEY (accountId) REFERENCES [Account](accountId)
    );
END
GO

-- 7. Bảng Backup (Sao lưu hệ thống)
IF OBJECT_ID(N'dbo.Backup', N'U') IS NULL
BEGIN
    CREATE TABLE [Backup] (
        backupId INT PRIMARY KEY,
        accountId INT,
        fileName NVARCHAR(255),
        storageLocation NVARCHAR(255),
        size FLOAT,
        backupTime DATETIME,
        status NVARCHAR(50),
        restoreTime DATETIME,
        FOREIGN KEY (accountId) REFERENCES [Account](accountId)
    );
END
GO

-- 8. Bảng ExchangeRate (Tỷ giá ngoại tệ)
IF OBJECT_ID(N'dbo.ExchangeRate', N'U') IS NULL
BEGIN
    CREATE TABLE [ExchangeRate] (
        currencyCode NVARCHAR(10) PRIMARY KEY,
        currencyName NVARCHAR(50),
        rate FLOAT,
        updatedDate DATETIME
    );
END
GO

-- 9. Bảng TransactionBank (Giao dịch ngân hàng)
IF OBJECT_ID(N'dbo.TransactionBank', N'U') IS NULL
BEGIN
    CREATE TABLE [TransactionBank] (
        idtrs NVARCHAR(50) PRIMARY KEY,
        ID VARCHAR(13) CHECK (LEN(ID)=13 AND ID NOT LIKE '%[^0-9]%'),
        amount DECIMAL(18,2),
        transactionType NVARCHAR(50),
        status NVARCHAR(50),
        transactionTime DATETIME,
        description NVARCHAR(255),
        isFlagged BIT,
        flagReason NVARCHAR(255),
        flaggedAt DATETIME,
        FOREIGN KEY (ID) REFERENCES [AccountBank](ID)
    );
END
GO

-- 10. Bảng TransactionLog (Nhật ký giao dịch)
IF OBJECT_ID(N'dbo.TransactionLog', N'U') IS NULL
BEGIN
    CREATE TABLE [TransactionLog] (
        logId BIGINT PRIMARY KEY,
        idtrs NVARCHAR(50),
        action NVARCHAR(100),
        timestamp DATETIME,
        result NVARCHAR(50),
        details NVARCHAR(MAX),
        FOREIGN KEY (idtrs) REFERENCES [TransactionBank](idtrs)
    );
END
GO

-- 11. Bảng TransactionLimit (Giới hạn giao dịch)
IF OBJECT_ID(N'dbo.TransactionLimit', N'U') IS NULL
BEGIN
    CREATE TABLE [TransactionLimit] (
        limitId INT PRIMARY KEY,
        idtrs NVARCHAR(50),
        minLimit FLOAT,
        maxLimit FLOAT,
        updatedDate DATETIME,
        FOREIGN KEY (idtrs) REFERENCES [TransactionBank](idtrs)
    );
END
GO

-- 12. Bảng Creditcard (Thẻ ngân hàng / Thẻ tín dụng)
IF OBJECT_ID(N'dbo.Creditcard', N'U') IS NULL
BEGIN
    CREATE TABLE [Creditcard] (
        idcard NVARCHAR(50) PRIMARY KEY,
        ID VARCHAR(13) CHECK (LEN(ID)=13 AND ID NOT LIKE '%[^0-9]%'),
        cardtype NVARCHAR(50),
        cardcolor NVARCHAR(50),
        cardstyle NVARCHAR(50),
        currentlimit INT,
        maxlimit INT,
        status NVARCHAR(50),
        FOREIGN KEY (ID) REFERENCES [AccountBank](ID)
    );
END
GO

-- 13. Bảng LoanProfile (Hồ sơ khoản vay & dư nợ)
IF OBJECT_ID(N'dbo.LoanProfile', N'U') IS NULL
BEGIN
    CREATE TABLE [LoanProfile] (
        idloan NVARCHAR(50) PRIMARY KEY,
        ID VARCHAR(13) CHECK (LEN(ID)=13 AND ID NOT LIKE '%[^0-9]%'),
        loanamount INT,
        interestrate FLOAT,
        loanterm INT,
        paidamount INT,
        remainamount INT,
        debtgroup NVARCHAR(20),
        creditscore INT,
        attachdocs NVARCHAR(255),
        status NVARCHAR(50),
        note NVARCHAR(500) NULL,
        approveddate DATE NULL,
        FOREIGN KEY (ID) REFERENCES [AccountBank](ID)
    );
END
GO

-- Bổ sung cột cho trường hợp chạy trên CSDL cũ đã tạo trước đó
IF COL_LENGTH('LoanProfile', 'note') IS NULL
    ALTER TABLE [LoanProfile] ADD note NVARCHAR(500) NULL;
GO
IF COL_LENGTH('LoanProfile', 'approveddate') IS NULL
    ALTER TABLE [LoanProfile] ADD approveddate DATE NULL;
GO

-- 14. Bảng StrategyReport (Báo cáo chiến lược)
IF OBJECT_ID(N'dbo.StrategyReport', N'U') IS NULL
BEGIN
    CREATE TABLE [StrategyReport] (
        idReport NVARCHAR(50) PRIMARY KEY,
        idtrs NVARCHAR(50),
        reportName NVARCHAR(100),
        reportDate DATE,
        reportType NVARCHAR(50),
        content NVARCHAR(MAX),
        FOREIGN KEY (idtrs) REFERENCES [TransactionBank](idtrs)
    );
END
GO

-- 15. Bảng ReportData (Dữ liệu báo cáo)
IF OBJECT_ID(N'dbo.ReportData', N'U') IS NULL
BEGIN
    CREATE TABLE [ReportData] (
        idData NVARCHAR(50) PRIMARY KEY,
        idReport NVARCHAR(50),
        dataType NVARCHAR(50),
        dataValue FLOAT,
        createdDate DATE,
        FOREIGN KEY (idReport) REFERENCES [StrategyReport](idReport)
    );
END
GO

-- 16. Bảng KPI
IF OBJECT_ID(N'dbo.KPI', N'U') IS NULL
BEGIN
    CREATE TABLE [KPI] (
        idKPI NVARCHAR(50) PRIMARY KEY,
        nameKPI NVARCHAR(100),
        valueKPI FLOAT,
        targetKPI FLOAT,
        evaluationDate DATE
    );
END
GO

-- 17. Bảng LoanApplicationRecord (Hồ sơ đăng ký vay)
IF OBJECT_ID(N'dbo.LoanApplicationRecord', N'U') IS NULL
BEGIN
    CREATE TABLE [LoanApplicationRecord] (
        idLoan NVARCHAR(50) PRIMARY KEY,
        ID VARCHAR(13) CHECK (LEN(ID)=13 AND ID NOT LIKE '%[^0-9]%'),
        loanAmount FLOAT,
        loanTerm INT,
        submissionDate DATE,
        status NVARCHAR(50),
        FOREIGN KEY (ID) REFERENCES [AccountBank](ID)
    );
END
GO

-- 18. Bảng StatisticsReport (Báo cáo thống kê)
IF OBJECT_ID(N'dbo.StatisticsReport', N'U') IS NULL
BEGIN
    CREATE TABLE [StatisticsReport] (
        reportId NVARCHAR(50) PRIMARY KEY,
        fromDate DATETIME,
        toDate DATETIME,
        idtrs NVARCHAR(50),
        statusFilter NVARCHAR(50),
        minAmount DECIMAL(18,2),
        maxAmount DECIMAL(18,2),
        groupBy NVARCHAR(50),
        chartType NVARCHAR(50),
        totalTransactions INT,
        totalAmount DECIMAL(18,2),
        createdAt DATETIME,
        FOREIGN KEY (idtrs) REFERENCES [TransactionBank](idtrs)
    );
END
GO

-- 19. Bảng BeneficiaryContact (Danh bạ thụ hưởng - UC Chuyển khoản)
IF OBJECT_ID(N'dbo.BeneficiaryContact', N'U') IS NULL
BEGIN
    CREATE TABLE [BeneficiaryContact] (
        idbnf INT IDENTITY(1,1) PRIMARY KEY,
        ID VARCHAR(13) NOT NULL CHECK (LEN(ID)=13 AND ID NOT LIKE '%[^0-9]%'),
        receiveraccount VARCHAR(20) NOT NULL,
        receivername NVARCHAR(300) NOT NULL,
        bankname NVARCHAR(100) NOT NULL,
        createddate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_Beneficiary UNIQUE (ID, receiveraccount, bankname),
        FOREIGN KEY (ID) REFERENCES [AccountBank](ID)
    );
END
GO

-- ====================================================================
-- PHẦN 2: DỮ LIỆU MẪU (Chạy lại không trùng lặp)
-- ====================================================================

-- 2.1 Tài khoản khách hàng (mật khẩu demo: 123456)
IF NOT EXISTS (SELECT 1 FROM AccountBank WHERE ID='1000000000001')
INSERT INTO AccountBank(ID,password,fullname,gender,birthday,CCCD,phone,address,email,type,providedate,money)
VALUES ('1000000000001','123456',N'Nguyễn Văn An',N'Nam','1995-03-12','079095001234','0901234567',N'12 Lê Lợi, Q.1, TP.HCM','an.nguyen@gmail.com',N'Tài khoản thanh toán','2023-01-10',50000000);

IF NOT EXISTS (SELECT 1 FROM AccountBank WHERE ID='1000000000002')
INSERT INTO AccountBank(ID,password,fullname,gender,birthday,CCCD,phone,address,email,type,providedate,money)
VALUES ('1000000000002','123456',N'Trần Thị Bình',N'Nữ','1998-07-25','079198002345','0912345678',N'45 Nguyễn Huệ, Q.1, TP.HCM','binh.tran@gmail.com',N'Tài khoản thanh toán','2023-05-20',20000000);

IF NOT EXISTS (SELECT 1 FROM AccountBank WHERE ID='1000000000003')
INSERT INTO AccountBank(ID,password,fullname,gender,birthday,CCCD,phone,address,email,type,providedate,money)
VALUES ('1000000000003','123456',N'Lê Văn Cường',N'Nam','1990-11-02','079090003456','0923456789',N'88 Võ Văn Tần, Q.3, TP.HCM','cuong.le@gmail.com',N'Tài khoản vay','2022-09-15',5000000);

IF NOT EXISTS (SELECT 1 FROM AccountBank WHERE ID='1000000000004')
INSERT INTO AccountBank(ID,password,fullname,gender,birthday,CCCD,phone,address,email,type,providedate,money)
VALUES ('1000000000004','123456',N'Phạm Thị Dung',N'Nữ','2000-01-30','079200004567','0934567890',N'21 Điện Biên Phủ, Bình Thạnh, TP.HCM','dung.pham@gmail.com',N'Tài khoản tiết kiệm','2024-02-01',100000);

IF NOT EXISTS (SELECT 1 FROM AccountBank WHERE ID='1000000000005')
INSERT INTO AccountBank(ID,password,fullname,gender,birthday,CCCD,phone,address,email,type,providedate,money)
VALUES ('1000000000005','123456',N'Hoàng Văn Em',N'Nam','1987-06-18','079087005678','0945678901',N'5 Phan Xích Long, Phú Nhuận, TP.HCM','em.hoang@gmail.com',N'Tài khoản vay','2021-12-12',80000000);
GO

-- 2.2 Thẻ tín dụng (hạn mức tối đa theo hạng: Chuẩn 50tr, Vàng 200tr, Bạch kim 500tr)
IF NOT EXISTS (SELECT 1 FROM Creditcard WHERE idcard='CC0001')
INSERT INTO Creditcard(idcard,ID,cardtype,cardcolor,cardstyle,currentlimit,maxlimit,status)
VALUES ('CC0001','1000000000001',N'Vàng',N'Vàng ánh kim',N'Tiêu chuẩn',100000000,200000000,N'Hoạt động');

IF NOT EXISTS (SELECT 1 FROM Creditcard WHERE idcard='CC0002')
INSERT INTO Creditcard(idcard,ID,cardtype,cardcolor,cardstyle,currentlimit,maxlimit,status)
VALUES ('CC0002','1000000000002',N'Chuẩn',N'Xanh dương',N'Trẻ trung',20000000,50000000,N'Hoạt động');

IF NOT EXISTS (SELECT 1 FROM Creditcard WHERE idcard='CC0003')
INSERT INTO Creditcard(idcard,ID,cardtype,cardcolor,cardstyle,currentlimit,maxlimit,status)
VALUES ('CC0003','1000000000003',N'Chuẩn',N'Đen',N'Tối giản',10000000,50000000,N'Đã khóa');

IF NOT EXISTS (SELECT 1 FROM Creditcard WHERE idcard='CC0004')
INSERT INTO Creditcard(idcard,ID,cardtype,cardcolor,cardstyle,currentlimit,maxlimit,status)
VALUES ('CC0004','1000000000005',N'Bạch kim',N'Bạc',N'Cao cấp',300000000,500000000,N'Hoạt động');
GO

-- 2.3 Hồ sơ vay & dư nợ (interestrate: %/năm, loanterm: tháng)
IF NOT EXISTS (SELECT 1 FROM LoanProfile WHERE idloan='LN0001')
INSERT INTO LoanProfile(idloan,ID,loanamount,interestrate,loanterm,paidamount,remainamount,debtgroup,creditscore,attachdocs,status,note,approveddate)
VALUES ('LN0001','1000000000001',100000000,8.5,24,40000000,60000000,N'Nhóm 1',720,N'CCCD.pdf;HopDongLaoDong.pdf;SaoKeLuong.pdf',N'Đã duyệt',N'Khách hàng uy tín','2025-06-01');

IF NOT EXISTS (SELECT 1 FROM LoanProfile WHERE idloan='LN0002')
INSERT INTO LoanProfile(idloan,ID,loanamount,interestrate,loanterm,paidamount,remainamount,debtgroup,creditscore,attachdocs,status,note,approveddate)
VALUES ('LN0002','1000000000003',50000000,10,12,15000000,35000000,N'Nhóm 3',540,N'CCCD.pdf;SaoKeLuong.pdf',N'Đã duyệt',N'Chậm trả 2 kỳ','2025-03-15');

IF NOT EXISTS (SELECT 1 FROM LoanProfile WHERE idloan='LN0003')
INSERT INTO LoanProfile(idloan,ID,loanamount,interestrate,loanterm,paidamount,remainamount,debtgroup,creditscore,attachdocs,status,note,approveddate)
VALUES ('LN0003','1000000000001',30000000,9,12,30000000,0,N'Nhóm 1',700,N'CCCD.pdf',N'Đã tất toán',N'Tất toán đúng hạn','2023-02-01');

IF NOT EXISTS (SELECT 1 FROM LoanProfile WHERE idloan='LN0004')
INSERT INTO LoanProfile(idloan,ID,loanamount,interestrate,loanterm,paidamount,remainamount,debtgroup,creditscore,attachdocs,status,note,approveddate)
VALUES ('LN0004','1000000000005',500000000,7.8,60,120000000,380000000,N'Nhóm 2',650,N'CCCD.pdf;GiayToNha.pdf',N'Đã duyệt',NULL,'2024-01-10');

IF NOT EXISTS (SELECT 1 FROM LoanProfile WHERE idloan='LN0005')
INSERT INTO LoanProfile(idloan,ID,loanamount,interestrate,loanterm,paidamount,remainamount,debtgroup,creditscore,attachdocs,status,note,approveddate)
VALUES ('LN0005','1000000000002',80000000,0,36,0,0,NULL,680,N'CCCD.pdf;HopDongLaoDong.pdf;SaoKeLuong.pdf',N'Chờ duyệt',NULL,NULL);

IF NOT EXISTS (SELECT 1 FROM LoanProfile WHERE idloan='LN0006')
INSERT INTO LoanProfile(idloan,ID,loanamount,interestrate,loanterm,paidamount,remainamount,debtgroup,creditscore,attachdocs,status,note,approveddate)
VALUES ('LN0006','1000000000004',20000000,0,12,0,0,NULL,600,N'CCCD.pdf',N'Chờ duyệt',NULL,NULL);
GO

-- 2.4 Hồ sơ đăng ký vay (đồng bộ mã với LoanProfile)
IF NOT EXISTS (SELECT 1 FROM LoanApplicationRecord WHERE idLoan='LN0001')
INSERT INTO LoanApplicationRecord(idLoan,ID,loanAmount,loanTerm,submissionDate,status)
VALUES ('LN0001','1000000000001',100000000,24,'2025-05-20',N'Đã duyệt');

IF NOT EXISTS (SELECT 1 FROM LoanApplicationRecord WHERE idLoan='LN0002')
INSERT INTO LoanApplicationRecord(idLoan,ID,loanAmount,loanTerm,submissionDate,status)
VALUES ('LN0002','1000000000003',50000000,12,'2025-03-01',N'Đã duyệt');

IF NOT EXISTS (SELECT 1 FROM LoanApplicationRecord WHERE idLoan='LN0003')
INSERT INTO LoanApplicationRecord(idLoan,ID,loanAmount,loanTerm,submissionDate,status)
VALUES ('LN0003','1000000000001',30000000,12,'2023-01-15',N'Đã duyệt');

IF NOT EXISTS (SELECT 1 FROM LoanApplicationRecord WHERE idLoan='LN0004')
INSERT INTO LoanApplicationRecord(idLoan,ID,loanAmount,loanTerm,submissionDate,status)
VALUES ('LN0004','1000000000005',500000000,60,'2023-12-20',N'Đã duyệt');

IF NOT EXISTS (SELECT 1 FROM LoanApplicationRecord WHERE idLoan='LN0005')
INSERT INTO LoanApplicationRecord(idLoan,ID,loanAmount,loanTerm,submissionDate,status)
VALUES ('LN0005','1000000000002',80000000,36,'2026-11-15',N'Chờ duyệt');

IF NOT EXISTS (SELECT 1 FROM LoanApplicationRecord WHERE idLoan='LN0006')
INSERT INTO LoanApplicationRecord(idLoan,ID,loanAmount,loanTerm,submissionDate,status)
VALUES ('LN0006','1000000000004',20000000,12,'2026-11-18',N'Chờ duyệt');
GO

-- 2.5 Danh bạ thụ hưởng mẫu
IF NOT EXISTS (SELECT 1 FROM BeneficiaryContact WHERE ID='1000000000001' AND receiveraccount='1000000000002')
INSERT INTO BeneficiaryContact(ID,receiveraccount,receivername,bankname)
VALUES ('1000000000001','1000000000002',N'Trần Thị Bình',N'Ngân hàng Nhóm 07 (nội bộ)');
GO

PRINT N'Khởi tạo cơ sở dữ liệu QuanLyGiaoDichNganHang và dữ liệu mẫu thành công!';
GO
