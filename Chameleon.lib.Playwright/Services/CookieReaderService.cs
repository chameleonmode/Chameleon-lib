using System.Data.SQLite;
using System.Diagnostics;

namespace Chameleon.lib.Playwright.Services
{
    public class PlaywrightCookie
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Domain { get; set; }
        public string? Path { get; set; }
        public long Expires { get; set; } // Unix timestamp in seconds
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public string? SameSite { get; set; } // e.g., "Lax", "Strict", "None"
    }

    public class CookieReaderService
    {
        private static CookieReaderService? _instance;
        public static CookieReaderService Instance => _instance ??= new CookieReaderService();

        public async Task<List<PlaywrightCookie>> ReadCookiesAsync(string sqliteFilePath)
        {
            var cookies = new List<PlaywrightCookie>();
            Debug.WriteLine($"[CookieReaderService] Attempting to read cookies from: {sqliteFilePath}");

            if (!File.Exists(sqliteFilePath))
            {
                Debug.WriteLine($"[CookieReaderService] Error: Cookie file not found at {sqliteFilePath}");
                return cookies;
            }

            var isFirefox = sqliteFilePath.EndsWith("cookies.sqlite", StringComparison.OrdinalIgnoreCase);
            var isChromium = sqliteFilePath.EndsWith("Cookies", StringComparison.OrdinalIgnoreCase);

            if (!isFirefox && !isChromium)
            {
                if (sqliteFilePath.Contains(Path.Combine("Mozilla", "Firefox"), StringComparison.OrdinalIgnoreCase))
                {
                    isFirefox = true;
                }
                else if (sqliteFilePath.Contains(Path.Combine("Google", "Chrome", "User Data"), StringComparison.OrdinalIgnoreCase) ||
                         sqliteFilePath.Contains(Path.Combine("Microsoft", "Edge", "User Data"), StringComparison.OrdinalIgnoreCase) ||
                         sqliteFilePath.Contains(Path.Combine("BraveSoftware", "Brave-Browser", "User Data"), StringComparison.OrdinalIgnoreCase) ||
                         (sqliteFilePath.Contains("Network") && Path.GetFileName(sqliteFilePath).Equals("Cookies", StringComparison.OrdinalIgnoreCase)))
                {
                    isChromium = true;
                }
                else
                {
                     Debug.WriteLine($"[CookieReaderService] Error: Could not determine browser type for cookie file: {sqliteFilePath}");
                     return cookies;
                }
            }


            var connectionString = $"Data Source={sqliteFilePath};Version=3;ReadOnly=True;";
            var sqlQuery = "";

            if (isFirefox)
            {
                sqlQuery = "SELECT name, value, host, path, expiry, isHttpOnly, isSecure, sameSite FROM moz_cookies";
            }
            else if (isChromium)
            {
                sqlQuery = "SELECT name, value, host_key, path, expires_utc, is_httponly, is_secure, samesite FROM cookies";
            }
            else
            {
                Debug.WriteLine($"[CookieReaderService] Error: Browser type not definitively identified for {sqliteFilePath}");
                return cookies;
            }

            try
            {
                using var connection = new SQLiteConnection(connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;

                using var reader = (SQLiteDataReader)await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long expires = -1;
                    if (isFirefox)
                    {
                        expires = reader.GetInt64(reader.GetOrdinal("expiry"));
                        if (expires == 0) expires = -1; 
                    }
                    else
                    {
                        var expiresUtc = reader.GetInt64(reader.GetOrdinal("expires_utc"));
                        if (expiresUtc > 0)
                        {
                            try
                            {
                                var dto = DateTimeOffset.FromFileTime(expiresUtc / 10);
                                expires = dto.ToUnixTimeSeconds();
                            }
                            catch (ArgumentOutOfRangeException) 
                            {
                                expires = -1;
                                Debug.WriteLine($"[CookieReaderService] Warning: Could not convert Chromium timestamp {expiresUtc} for cookie {reader.GetString(reader.GetOrdinal("name"))}");
                            }
                        }
                        else
                        {
                            expires = -1; 
                        }
                    }

                    var sameSiteString = "None";
                    if (isFirefox)
                    {
                        var sameSiteInt = reader.IsDBNull(reader.GetOrdinal("sameSite")) ? 0 : (int)reader.GetInt64(reader.GetOrdinal("sameSite"));
                        sameSiteString = sameSiteInt switch
                        {
                            1 => "Lax",
                            2 => "Strict",
                            _ => "None",
                        };
                    }
                    else 
                    {
                        var sameSiteInt = reader.IsDBNull(reader.GetOrdinal("samesite")) ? -1 : reader.GetInt32(reader.GetOrdinal("samesite"));
                        sameSiteString = sameSiteInt switch
                        {
                            1 => "Lax",
                            2 => "Strict",
                            _ => "None",
                        };
                    }
                    
                    cookies.Add(new PlaywrightCookie
                    {
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Value = reader.GetString(reader.GetOrdinal("value")),
                        Domain = reader.GetString(reader.GetOrdinal(isFirefox ? "host" : "host_key")),
                        Path = reader.GetString(reader.GetOrdinal("path")),
                        Expires = expires,
                        HttpOnly = reader.GetInt64(reader.GetOrdinal(isFirefox ? "isHttpOnly" : "is_httponly")) == 1,
                        Secure = reader.GetInt64(reader.GetOrdinal(isFirefox ? "isSecure" : "is_secure")) == 1,
                        SameSite = sameSiteString
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CookieReaderService] Exception while reading cookies from {sqliteFilePath}: {ex.Message}");
							  throw;
						}
            
            Debug.WriteLine($"[CookieReaderService] Successfully read {cookies.Count} cookies from {sqliteFilePath}");
            return cookies;
        }
    }
}
