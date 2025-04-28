-- InsertCustomer: Inserts a new customer record and returns the generated CustomerId.
CREATE OR ALTER PROCEDURE [dbo].[InsertCustomer]
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Email NVARCHAR(150),
    @NewCustomerId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            INSERT INTO dbo.Customer (FirstName, LastName, Email, CreatedDate)
            VALUES (@FirstName, @LastName, @Email, GETDATE());

            SET @NewCustomerId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

-- AddCustomerAddress: Inserts a new address record for a customer.
CREATE OR ALTER PROCEDURE [dbo].[AddCustomerAddress]
    @CustomerId    INT,
    @StreetAddress NVARCHAR(200),
    @ZipCode       NVARCHAR(10),
    @City          NVARCHAR(100),
    @State         CHAR(2),
    @NewAddressId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            -- Check if the zip code already exists in the ZipCode table.
            IF NOT EXISTS (SELECT 1 FROM dbo.ZipCode WHERE ZipCode = @ZipCode)
            BEGIN
                INSERT INTO dbo.ZipCode (ZipCode, City, State)
                VALUES (@ZipCode, @City, @State);
            END;

            -- Insert the address record linked to the customer.INSERT INTO dbo.Address (CustomerId, StreetAddress, ZipCode)
            INSERT INTO dbo.Address (CustomerId, StreetAddress, ZipCode)
            VALUES (@CustomerId, @StreetAddress, @ZipCode);
            SET @NewAddressId = SCOPE_IDENTITY();


        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

-- AddCustomerPhone: Inserts a new phone record for a customer, inserting a new phone type if necessary.
CREATE OR ALTER PROCEDURE [dbo].[AddCustomerPhone]
    @CustomerId    INT,
    @PhoneNumber   NVARCHAR(20),
    @PhoneType NVARCHAR(50),
    @NewPhoneId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            DECLARE @PhoneTypeId INT;

            -- Check if the phone type exists; if not, insert it.
            IF NOT EXISTS (SELECT 1 FROM dbo.PhoneType WHERE PhoneType = @PhoneType)
            BEGIN
                INSERT INTO dbo.PhoneType (PhoneType)
                VALUES (@PhoneType);
                SET @PhoneTypeId = SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                SELECT @PhoneTypeId = PhoneTypeId FROM dbo.PhoneType WHERE PhoneType = @PhoneType;
            END

            -- Now insert the customer's phone record.
            INSERT INTO dbo.CustomerPhone (CustomerId, PhoneNumber, PhoneTypeId)
            VALUES (@CustomerId, @PhoneNumber, @PhoneTypeId);
            SET @NewPhoneId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

-- AddCustomerOrder: Creates a new order for a customer.
CREATE OR ALTER PROCEDURE [dbo].[AddCustomerOrder]
    @CustomerId  INT,
    @OrderDate   DATETIME = NULL,
    @NewOrderId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @OrderDate IS NULL
        SET @OrderDate = GETDATE();

    BEGIN TRY
        BEGIN TRANSACTION;

            INSERT INTO dbo.[Order] (CustomerId, OrderDate)
                VALUES (@CustomerId, @OrderDate);
            SET @NewOrderId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO


CREATE OR ALTER PROCEDURE [dbo].[AddOrderItem]
    @OrderId         INT,
    @ProductId       INT,
    @Quantity        INT,
    @UnitPrice       DECIMAL(10,2),
    @NewOrderItemId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            INSERT INTO dbo.OrderItem (OrderId, ProductId, Quantity, UnitPrice)
            VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);

            SET @NewOrderItemId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO


CREATE OR ALTER PROCEDURE [dbo].[InsertProduct]
    @ProductName NVARCHAR(150),
    @Description NVARCHAR(MAX) = NULL,
    @Price DECIMAL(10,2),
    @CategoryName NVARCHAR(100),
    @NewProductId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            DECLARE @CategoryId INT;

            -- Check if the product category exists; if not, insert it.
            IF NOT EXISTS (SELECT 1 FROM dbo.ProductCategory WHERE CategoryName = @CategoryName)
            BEGIN
                INSERT INTO dbo.ProductCategory (CategoryName)
                VALUES (@CategoryName);
                SET @CategoryId = SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                SELECT @CategoryId = CategoryId
                FROM dbo.ProductCategory
                WHERE CategoryName = @CategoryName;
            END

            -- Insert the product record using the CategoryId:
            INSERT INTO dbo.Product (ProductName, Description, Price, CategoryId)
            VALUES (@ProductName, @Description, @Price, @CategoryId);

            -- Retrieve the newly generated ProductId:
            SET @NewProductId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Trigger for Customer Table
CREATE OR ALTER TRIGGER trg_Customer_Delete
ON dbo.Customer
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Customer_Archive (CustomerId, FirstName, LastName, Email, CreatedDate, DeletedDate)
    SELECT CustomerId, FirstName, LastName, Email, CreatedDate, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for PhoneType Table
CREATE OR ALTER TRIGGER trg_PhoneType_Delete
ON dbo.PhoneType
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PhoneType_Archive (PhoneTypeId, PhoneType, DeletedDate)
    SELECT PhoneTypeId, PhoneType, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for ZipCode Table
CREATE OR ALTER TRIGGER trg_ZipCode_Delete
ON dbo.ZipCode
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ZipCode_Archive (ZipCode, City, State, DeletedDate)
    SELECT ZipCode, City, State, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for ProductCategory Table
CREATE OR ALTER TRIGGER trg_ProductCategory_Delete
ON dbo.ProductCategory
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ProductCategory_Archive (CategoryId, CategoryName, DeletedDate)
    SELECT CategoryId, CategoryName, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for Product Table
CREATE OR ALTER TRIGGER trg_Product_Delete
ON dbo.Product
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Product_Archive (ProductId, ProductName, Description, Price, CategoryId, DeletedDate)
    SELECT ProductId, ProductName, Description, Price, CategoryId, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for Order Table
CREATE OR ALTER TRIGGER trg_Order_Delete
ON dbo.[Order]
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Order_Archive (OrderId, CustomerId, OrderDate, DeletedDate)
    SELECT OrderId, CustomerId, OrderDate, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for OrderItem Table
CREATE OR ALTER TRIGGER trg_OrderItem_Delete
ON dbo.OrderItem
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OrderItem_Archive (OrderItemId, OrderId, ProductId, Quantity, UnitPrice, DeletedDate)
    SELECT OrderItemId, OrderId, ProductId, Quantity, UnitPrice, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for Address Table
CREATE OR ALTER TRIGGER trg_Address_Delete
ON dbo.Address
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Address_Archive (AddressId, CustomerId, StreetAddress, ZipCode, DeletedDate)
    SELECT AddressId, CustomerId, StreetAddress, ZipCode, GETDATE()
    FROM DELETED;
END;
GO

-- Trigger for CustomerPhone Table
CREATE OR ALTER TRIGGER trg_CustomerPhone_Delete
ON dbo.CustomerPhone
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.CustomerPhone_Archive (PhoneId, CustomerId, PhoneNumber, PhoneTypeId, DeletedDate)
    SELECT PhoneId, CustomerId, PhoneNumber, PhoneTypeId, GETDATE()
    FROM DELETED;
END;
GO
