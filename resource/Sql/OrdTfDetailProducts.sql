-- OrdTfDetailProducts : product ids of the lines the order has now (EV-ORD-308 step 4-3: which rows existed)
SELECT
  ProductID
FROM
  [Order Details]
WHERE
  OrderID = @OrderID
