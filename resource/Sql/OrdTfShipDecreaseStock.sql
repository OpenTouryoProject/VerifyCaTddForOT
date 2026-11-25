-- OrdTfShipDecreaseStock : subtract the quantity from the stock, RowVersion + 1 (EV-ORD-402 step 4-3, OD-T5, PRD-T5. DEC-ORD-59)
-- The condition keeps the stock from going negative even when another request changes it after the check
UPDATE
  Products
SET
  UnitsInStock = UnitsInStock - @Quantity,
  RowVersion = RowVersion + 1
WHERE
  ProductID = @ProductID
  AND (UnitsInStock IS NULL OR UnitsInStock >= @Quantity)
