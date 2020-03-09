set nocount on;
PRINT 'MsSQL - checking for permissions:'
PRINT '--------------------------------'
DECLARE @ok char(300), @ko char(300)
SET @ok = 'Current user''s login [' + SYSTEM_USER + '] is a member of the sysadmin role'
SET @ko = 'Current user''s login [' + SYSTEM_USER + '] is NOT a member of the sysadmin role'
IF IS_SRVROLEMEMBER ('sysadmin') = 1  
   select @ok  
ELSE IF IS_SRVROLEMEMBER ('sysadmin') = 0  
   select @ko
ELSE IF IS_SRVROLEMEMBER ('sysadmin') IS NULL  
   print 'ERROR: The server role specified is not valid.';
GO
PRINT '--------------------------------'