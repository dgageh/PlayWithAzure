-- InsertCustomer: Inserts a new customer record and returns the generated CustomerId.
CREATE OR ALTER PROCEDURE [dbo].[InsertCustomer]
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Email NVARCHAR(150),
    @NewCustomerId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            -- Generate a new Customer ID.
            SET @NewCustomerId = NEWID();

            INSERT INTO dbo.Customer (CustomerId, FirstName, LastName, Email, CreatedDate)
            VALUES (@NewCustomerId, @FirstName, @LastName, @Email, GETDATE());

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
    @CustomerId    UNIQUEIDENTIFIER,
    @StreetAddress NVARCHAR(200),
    @ZipCode       NVARCHAR(10),
    @City          NVARCHAR(100),
    @State         CHAR(2)
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

            -- Insert the address record linked to the customer.
            INSERT INTO dbo.Address (AddressId, CustomerId, StreetAddress, ZipCode)
            VALUES (NEWID(), @CustomerId, @StreetAddress, @ZipCode);

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
    @CustomerId    UNIQUEIDENTIFIER,
    @PhoneNumber   NVARCHAR(20),
    @PhoneTypeName NVARCHAR(50),
    @NewPhoneId    UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            DECLARE @PhoneTypeId INT;  -- PhoneType table uses an INT identity.
            
            -- Check if the phone type exists; if not, insert it.
            IF NOT EXISTS (SELECT 1 FROM dbo.PhoneType WHERE PhoneTypeName = @PhoneTypeName)
            BEGIN
                INSERT INTO dbo.PhoneType (PhoneTypeName)
                VALUES (@PhoneTypeName);
                SET @PhoneTypeId = SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                SELECT @PhoneTypeId = PhoneTypeId FROM dbo.PhoneType WHERE PhoneTypeName = @PhoneTypeName;
            END

            -- Now insert the customer's phone record.
            SET @NewPhoneId = NEWID();
            INSERT INTO dbo.CustomerPhone (PhoneId, CustomerId, PhoneNumber, PhoneTypeId)
            VALUES (@NewPhoneId, @CustomerId, @PhoneNumber, @PhoneTypeId);

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
    @CustomerId  UNIQUEIDENTIFIER,
    @OrderDate   DATETIME = NULL,
    @NewOrderId  UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @OrderDate IS NULL
        SET @OrderDate = GETDATE();

    BEGIN TRY
        BEGIN TRANSACTION;

            SET @NewOrderId = NEWID();
            INSERT INTO dbo.[Order] (OrderId, CustomerId, OrderDate)
            VALUES (@NewOrderId, @CustomerId, @OrderDate);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

-- AddOrderItem: Inserts a new order item for an order.
-- Note: Since OrderItemId is defined as an INT identity and ProductId is an INT in the Product table,
-- we ensure the parameters reflect these types. Also, UnitPrice is passed as an input.
CREATE OR ALTER PROCEDURE [dbo].[AddOrderItem]
    @OrderId         UNIQUEIDENTIFIER,
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