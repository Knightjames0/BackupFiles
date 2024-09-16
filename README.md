# BackupFiles V1.2.0

Simple command line tool for backing up data to external or local stoarge. As well maintains a list of file paths for future backups.


# List of Commands

add [file...] - Adds file paths or directory paths to backup. For file paths with spaces inclose with ("").

remove [file...] - Removes file paths or directory paths from backup. For file paths with spaces inclose with ("").

list - Provides a sorted list of all paths added

backup -[options] [file] - Creates one of all the files add the inputed location and must have a destination file path.

backup -n [file] [file...] - Option, Creates one of all the files add the inputed location and copies only ones that don't exist in other backups.

backup -c [file] - Option, Check for any Directories in the same folder that are old backups and copies only ones that don't exist in other backups.

backup -z [file] - Option, Compress the backup to zip file seperated by drive.

version - Displays Version number.
