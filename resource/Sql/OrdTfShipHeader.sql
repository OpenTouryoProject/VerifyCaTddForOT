-- OrdTfShipHeader : one order with its customer name for the shipping dialog (EV-ORD-401 steps 1-3, EV-ORD-402 validation)
SELECT
  o.OrderID,
  c.CompanyName AS CustomerName,
  o.OrderDate,
  o.RequiredDate,
  o.ShippedDate,
  o.ShipVia,
  o.Freight,
  o.RowVersion
FROM
  Orders o
  LEFT JOIN Customers c ON c.CustomerID = o.CustomerID
WHERE
  o.OrderID = @OrderID
