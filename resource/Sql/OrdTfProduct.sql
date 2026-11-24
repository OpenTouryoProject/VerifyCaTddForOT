-- OrdTfProduct : one product (EV-ORD-306 step 3, EV-ORD-308 VAL-EXISTS)
SELECT
  ProductName, UnitPrice, UnitsInStock, Discontinued
FROM
  Products
WHERE
  ProductID = @ProductID
