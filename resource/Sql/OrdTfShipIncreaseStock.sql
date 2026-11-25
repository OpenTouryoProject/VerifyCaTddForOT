-- OrdTfShipIncreaseStock : add the quantity to the stock, RowVersion + 1 (EV-ORD-403 step 5, OD-T5. DEC-ORD-59)
UPDATE
  Products
SET
  UnitsInStock = UnitsInStock + @Quantity,
  RowVersion = RowVersion + 1
WHERE
  ProductID = @ProductID
