using System.Collections.Frozen;

namespace BotGuard.Gateway.Services;

public class HoneypotInspector
{
    private static readonly FrozenSet<string> FrozenSet =
    [
        "/.env", "/.env.local", "/.env.dev", "/.env.prod", "/.env.production", "/.env.bak", "/.env.example",
        "/.git/config", "/.git/HEAD", "/.svn/entries", "/.hg/hgrc",
        "/.aws/credentials", "/.aws/config", "/.ssh/id_rsa", "/.ssh/authorized_keys",
        "/config.json", "/config.php", "/config.yml", "/config.yaml", "/configuration.php", "/appsettings.json",
        "/appsettings.Development.json",
        "/web.config", "/.htaccess", "/.htpasswd", "/.DS_Store",
        
        "/wp-admin", "/wp-login.php", "/wp-admin/setup.php", "/wp-config.php", "/wp-config.php.bak",
        "/wp-config.php.save",
        "/xmlrpc.php", "/wp-content/debug.log", "/wp-json/wp/v2/users", "/wp-cron.php",
        "/wp-includes/wlwmanifest.xml", "/wp-content/plugins/", "/wp-content/uploads/", "/wp-content/backup-db/",

        "/administrator/", "/administrator/index.php", "/user/login", "/user/register", "/sites/default/settings.php",
        "/bitrix/admin/", "/bitrix/php_interface/dbconn.php", "/bitrix/.settings.php",
        "/admin/config.php", "/magento_version", "/downloader/", "/index.php/admin",
        "/admin/index.php", "/catalog/admin/", "/system/storage/", "/vqmod/",

        "/phpmyadmin", "/phpmyadmin/", "/phpMyAdmin/", "/pma/", "/adminer.php", "/adminer/",
        "/cpanel", "/webmail", "/admin.php", "/admin/", "/admin/login", "/login", "/login.php",
        "/manager/html", "/manager/status", "/host-manager/html", "/console", "/jmx-console",

        "/backup.zip", "/backup.tar.gz", "/backup.tar", "/backup.rar", "/backup.7z",
        "/db.sql", "/dump.sql", "/database.sql", "/backup.sql", "/site.sql", "/data.sql",
        "/www.zip", "/site.zip", "/web.zip", "/archive.zip", "/html.zip", "/public_html.zip",
        "/1.zip", "/a.zip", "/test.zip", "/backup/", "/backups/", "/db_backup/",

        "/server-status", "/server-info", "/phpinfo.php", "/info.php", "/test.php", "/debug.php",
        "/actuator", "/actuator/env", "/actuator/health", "/swagger-ui.html", "/v2/api-docs", "/swagger.json",
        "/trace", "/metrics", "/env", "/health", "/console/",

        "/shell.php", "/cmd.php", "/upload.php", "/eval.php", "/c99.php", "/r57.php", "/webshell.php",
        "/etc/passwd", "/etc/shadow", "/proc/self/environ", "/cgi-bin/", "/cgi-bin/test.cgi",
        "/vendor/phpunit/phpunit/src/Util/PHP/eval-stdin.php", "/.well-known/security.txt",

        "/logs/", "/log/", "/error.log", "/access.log", "/debug.log", "/app.log", "/laravel.log",
        "/storage/logs/laravel.log", "/runtime/log/", "/var/log/", "/tmp/"
    ];


    public bool IsTrapPath(string path)
    {
        return FrozenSet.Contains(path);
    }
}