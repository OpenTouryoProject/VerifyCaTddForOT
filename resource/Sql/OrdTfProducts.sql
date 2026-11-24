-- OrdTfProducts : product choices for SC-ORD-03 (not discontinued, ProductName ascending. OD-T3)
SELECT
  ProductID, ProductName
FROM
  Products
WHERE
  Discontinued = 0
ORDER BY
  ProductName ASC, ProductID ASC
