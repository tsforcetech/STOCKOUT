-- Template: Permissions
-- REPLACE , 
USE [];
GO
CREATE USER [] FOR LOGIN [];
ALTER ROLE [db_datareader] ADD MEMBER [];
ALTER ROLE [db_datawriter] ADD MEMBER [];
GRANT EXECUTE TO [];
GO
