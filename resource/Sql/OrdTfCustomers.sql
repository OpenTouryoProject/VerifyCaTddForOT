-- OrdTfCustomers : customer choices for SC-ORD-01 (CompanyName ascending)
SELECT
  CustomerID, CompanyName
FROM
  Customers
ORDER BY
  CompanyName ASC, CustomerID ASC
