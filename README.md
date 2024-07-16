# BackupFiles V1.1.3

Simple command line tool for backing up data to external or local stoarge. As well maintains a list of file paths for future backups.


# List of Commands

add [file...] - Adds file paths or directory paths to backup. For file paths with spaces inclose with ("").

remove [file...] - Removes file paths or directory paths from backup. For file paths with spaces inclose with ("").

list - Provides a sorted list of all paths added

Backup [file] - Creates one of all the files add at a inputed location and must have a destination file path.

backup -n [file] [file...] - Creates one of all the files add the inputed location and copies only ones that don't exist in other backups.

version - Displays Version number.
