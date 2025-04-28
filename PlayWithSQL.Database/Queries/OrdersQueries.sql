
SELECT Customer.CustomerId, FirstName, LastName, sum(OrderTotal) AS OrderTotal, COUNT([Order].OrderId) As OrderCount FROM Customer
	JOIN dbo.[Order] ON Customer.CustomerId = [Order].CustomerId
	JOIN (
		SELECT OrderId, SUM(Quantity * UnitPrice) AS OrderTotal FROM dbo.OrderItem 
			JOIN dbo.Product ON OrderItem.ProductId = Product.ProductId
			GROUP BY OrderId
		) AS OrderTotal ON dbo.[Order].OrderId = OrderTotal.OrderId
	WHERE Customer.CustomerId = 6
	GROUP BY Customer.CustomerId, FirstName, LastName
	ORDER BY OrderTotal DESC


