-- OrdTfUpdateDetail : update one order line by OrderID and ProductID, RowVersion + 1 (EV-ORD-308 step 4-3)
UPDATE
  [Order Details]
SET
  UnitPrice = @UnitPrice,
  Quantity = @Quantity,
  Discount = @Discount,
  RowVersion = RowVersion + 1
WHERE
  OrderID = @OrderID
  AND ProductID = @ProductID
