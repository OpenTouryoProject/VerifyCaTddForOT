-- OrdTfEditLines : order lines with products for edit (EV-ORD-301 step 3-4). Discount is converted to decimal
SELECT
  od.ProductID,
  p.ProductName,
  p.Discontinued,
  p.UnitsInStock,
  od.UnitPrice,
  CAST(od.Discount AS decimal(5, 4)) AS Discount,
  od.Quantity,
  od.RowVersion
FROM
  [Order Details] od
  INNER JOIN Products p ON p.ProductID = od.ProductID
WHERE
  od.OrderID = @OrderID
ORDER BY
  p.ProductName ASC, od.ProductID ASC
