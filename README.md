# Sha1Sum
Quickly generate a SHA1 hash of a file or folder in Windows.  There are several tools out there that do some of what this tool can do.  I mainly needed a small appliction to generate a SHA1 hash of an entire directory, while omitting certain file types.  

# Usage
Sha1Sum takes several parameters:<br />
<b>-p, --path={path to directory or file}</b> - This should be a full path without a trailing slash.  Note that C:\test and C:\test\ will produce differnet hashes. - <i>required</i><br />
<b>-r, --recursive</b> - Recursively check subdirectories. - <i>Disabled by default</i><br />
<b>-e, --exclude=</b> - Comma separated list of file extensions to exclude, ex .log,.txt<br />
<b>-s, --stripquotes</b> - Enable this when you are using a monitoring system, such as zabbix, to call the application.  The system may add quotes to the --path value which may throw an error<br />
<b>-d, --debug</b> - Writes debug output to the console window

# Output
If the folder or file exists, the output will be a SHA1 hash of that object.  If the object was not found or access denied, the output will be -1
