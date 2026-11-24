-- OrdTfDeleteDetail : delete one order line by OrderID and ProductID (EV-ORD-308 step 4-3)
DELETE FROM
  [Order Details]
WHERE
  OrderID = @OrderID
  AND ProductID = @ProductID
