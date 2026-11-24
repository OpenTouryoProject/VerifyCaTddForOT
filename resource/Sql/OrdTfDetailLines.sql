-- OrdTfDetailLines : order lines with products (EV-ORD-201 step 3). Discount is converted to decimal (real is not used for amounts)
SELECT
  od.ProductID,
  p.ProductName,
  p.Discontinued,
  p.UnitsInStock,
  od.UnitPrice,
  CAST(od.Discount AS decimal(5, 4)) AS Discount,
  od.Quantity
FROM
  [Order Details] od
  INNER JOIN Products p ON p.ProductID = od.ProductID
WHERE
  od.OrderID = @OrderID
ORDER BY
  p.ProductName ASC, od.ProductID ASC
