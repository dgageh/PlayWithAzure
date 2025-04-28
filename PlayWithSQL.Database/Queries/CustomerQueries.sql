-- Asked AI to give some queries to write

-- 1
SELECT CustomerId, FirstName, LastName, CreatedDate from Customer

-- 2
SELECT ProductName, Price FROM Product where Price > 50
	ORder by Price DESC
-- 3
SELECT c.CustomerId, Firstname, LastName, Count(o.OrderID) As OrderCount FROM Customer c
	Join [Order] o on c.CustomerId = o.CustomerId
	Group By c.CustomerId, FirstName, LastName
	Order by OrderCount desc

-- 4
SELECT oi.OrderId, ProductName, pc.CategoryName from OrderItem oi
	JOIN Product p on oi.ProductId = p.ProductId
	JOIN ProductCategory pc on pc.CategoryId = p.CategoryId
	where OrderId = 6

-- 5
SELECT StreetAddress, City, State, a.ZipCode FROM Address  a
	LEFT JOIN ZipCode z on a.ZipCode = z.ZipCode

-- 6
SELECT * From Customer c
	JOIN dbo.[Order] o on c.CustomerId = o.CustomerId

-- 7 
SELECT p.ProductId, p.ProductName, OrderDetail.TotalOrder  FROM Product p
	JOIN (	
		SELECT ProductId, Sum(Quantity * UnitPrice) As TotalOrder FROM OrderItem oi
			Group by ProductID
		) AS OrderDetail on p.ProductId = OrderDetail.ProductId
	Order by OrderDetail.TotalOrder Desc

--8
Select * from Customer where Customer.CustomerId in 
(
	SELECT distinct Customer.CustomerId from Customer c
		Join [Order] o on c.CustomerId = o.CustomerId
		Join [OrderItem] oi on o.OrderId = oi.OrderId
		WHere oi.ProductId = 32
)
--9 
Select c.*, p.PhoneNumber, pt.PhoneType from customer c
	Join CustomerPhone p on c.CustomerId = p.CustomerId
	Join PhoneType pt on pt.PhoneTypeId = p.PhoneTypeId
	where c.CustomerId = 8
-- 10

SELECT p.ProductName, TopProduct.QtyOrdered From Product p
Join (
	SELECT TOP 1 oi.ProductId, Sum(Quantity) AS QtyOrdered from OrderItem oi
		GROUP BY oi.ProductId
		ORDER BY QtyOrdered DESC
	) AS TopProduct ON p.ProductId = TopProduct.ProductId
