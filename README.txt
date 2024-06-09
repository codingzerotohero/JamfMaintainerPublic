This is a Windows service I wrote which is currently in production for some customer which use Jamf School for device management of students and teachers iPads on different schools.

The real fun happens in Processor.cs - if you want to see what it's actually doing.
-Program.cs is mainly just starting up in Service mode or in Console Mode.

It gets user information from specified SQL tables, processes them and applies business logic depending on role etc, then creates/updates them in the Jamf API, as well as keeping track of the users and groups by storing them in an SQLite archive, so we can fetch users from there when we want to update them in the Jamf API as we need to have the Jamf generated user ID whenever we want to update a user.

Also includes an AppSettings.xml for certain sensitive values which can change depending on the customer.

Can run in two different ways:
-Console mode (which has several different use cases depending on what you want to do)
-Service mode (listens to SQL Service broker trigger and updates/creates users based on the message from the queue, which is triggered any time a row in the SQL table is either updated or inserted.

This public version will probably not run unless the correct values are supplied in the appsettings.xml, so don't expect it to work for you.