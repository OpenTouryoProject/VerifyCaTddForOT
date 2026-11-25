-- OrdTfShipStockLines : order lines with product name, quantity and stock (EV-ORD-401 step 5-4, EV-ORD-402 step 4-1, EV-ORD-403 step 5. Products is an inner join)
SELECT
  p.ProductID,
  p.ProductName,
  od.Quantity,
  p.UnitsInStock
FROM
  [Order Details] od
  INNER JOIN Products p ON p.ProductID = od.ProductID
WHERE
  od.OrderID = @OrderID
ORDER BY
  p.ProductID ASC
