using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using System.Text.RegularExpressions;
using System.Text;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class SqlToolsController : Controller
    {
        private readonly AppDbContext _db;
        public SqlToolsController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var history = await _db.SqlHistories
                .Include(s => s.Task)
                .OrderByDescending(s => s.CreatedAt)
                .Take(30)
                .ToListAsync();

            ViewBag.Tasks = await _db.Tasks
                .OrderByDescending(t => t.UpdatedAt)
                .Take(50)
                .Select(t => new { t.Id, t.Title })
                .ToListAsync();

            return View(history);
        }

        [HttpPost]
        public IActionResult Format([FromBody] SqlFormatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, error = "Input SQL tidak boleh kosong" });

            try
            {
                var formatted = SqlFormatterEngine.Format(req.Content, req);
                var origBytes = Encoding.UTF8.GetByteCount(req.Content);
                var formBytes = Encoding.UTF8.GetByteCount(formatted);
                var lines = formatted.Split('\n').Length;

                return Json(new
                {
                    success = true,
                    result = formatted,
                    dialect = req.Dialect ?? "sql",
                    originalSize = origBytes,
                    formattedSize = formBytes,
                    lines = lines
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Format SQL gagal: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult Minify([FromBody] SqlMinifyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, error = "Input SQL tidak boleh kosong" });

            try
            {
                var minified = SqlFormatterEngine.Minify(req.Content);
                var origBytes = Encoding.UTF8.GetByteCount(req.Content);
                var minBytes = Encoding.UTF8.GetByteCount(minified);
                var ratio = origBytes > 0 ? Math.Round((1.0 - (double)minBytes / origBytes) * 100.0, 1) : 0;

                return Json(new
                {
                    success = true,
                    result = minified,
                    originalSize = origBytes,
                    minifiedSize = minBytes,
                    compressionRatio = ratio
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Minify SQL gagal: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult Validate([FromBody] SqlValidateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, error = "Input SQL tidak boleh kosong" });

            var result = SqlFormatterEngine.Validate(req.Content);
            if (result.IsValid)
            {
                return Json(new { success = true, message = "Struktur dasar sintaks SQL valid!" });
            }
            return Json(new { success = false, error = result.Error, line = result.LineNumber });
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveSqlRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, error = "Konten SQL tidak boleh kosong" });

            var item = new SqlHistory
            {
                Name = string.IsNullOrWhiteSpace(req.Name) ? $"SQL Query {DateTime.Now:dd/MM HH:mm}" : req.Name.Trim(),
                Content = req.Content,
                Dialect = string.IsNullOrWhiteSpace(req.Dialect) ? "sql" : req.Dialect.Trim(),
                TaskId = req.TaskId,
                CreatedAt = DateTime.Now
            };

            _db.SqlHistories.Add(item);
            await _db.SaveChangesAsync();

            return Json(new { success = true, id = item.Id, name = item.Name });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.SqlHistories.FindAsync(id);
            if (item != null)
            {
                _db.SqlHistories.Remove(item);
                await _db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            var item = await _db.SqlHistories.Include(s => s.Task).FirstOrDefaultAsync(s => s.Id == id);
            if (item == null) return Json(new { success = false, error = "Data tidak ditemukan" });

            return Json(new
            {
                success = true,
                id = item.Id,
                content = item.Content,
                name = item.Name,
                dialect = item.Dialect,
                taskId = item.TaskId,
                taskTitle = item.Task?.Title
            });
        }
    }

    public class SqlFormatRequest
    {
        public string Content { get; set; } = string.Empty;
        public string Dialect { get; set; } = "sql";
        public int IndentSize { get; set; } = 2;
        public bool UseTabs { get; set; } = false;
        public string KeywordCase { get; set; } = "upper"; // upper, lower, preserve
        public string IdentifierCase { get; set; } = "preserve";
        public int LinesBetweenQueries { get; set; } = 1;
    }

    public class SqlMinifyRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class SqlValidateRequest
    {
        public string Content { get; set; } = string.Empty;
        public string Dialect { get; set; } = "sql";
    }

    public class SaveSqlRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Dialect { get; set; } = "sql";
        public int? TaskId { get; set; }
    }

    public static class SqlFormatterEngine
    {
        private static readonly HashSet<string> MajorKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "GROUP BY", "HAVING", "ORDER BY", "LIMIT", "OFFSET",
            "INSERT INTO", "VALUES", "UPDATE", "SET", "DELETE FROM", "DELETE", "MERGE INTO",
            "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "FULL JOIN", "CROSS JOIN", "JOIN",
            "ON", "USING", "UNION ALL", "UNION", "EXCEPT", "INTERSECT",
            "CREATE TABLE", "ALTER TABLE", "DROP TABLE", "CREATE INDEX", "DROP INDEX", "CREATE VIEW",
            "WITH RECURSIVE", "WITH", "WINDOW", "QUALIFY",
            "BEGIN TRANSACTION", "COMMIT", "ROLLBACK", "START TRANSACTION"
        };

        private static readonly HashSet<string> AllKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "DISTINCT", "AS", "FROM", "WHERE", "AND", "OR", "NOT", "IN", "BETWEEN",
            "LIKE", "ILIKE", "IS", "NULL", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC",
            "LIMIT", "OFFSET", "TOP", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS",
            "ON", "USING", "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE", "CREATE", "ALTER",
            "DROP", "TABLE", "VIEW", "INDEX", "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "CHECK",
            "DEFAULT", "UNIQUE", "CONSTRAINT", "CASCADE", "UNION", "ALL", "EXCEPT", "INTERSECT",
            "CASE", "WHEN", "THEN", "ELSE", "END", "COALESCE", "NULLIF", "CAST", "EXISTS",
            "OVER", "PARTITION", "ROW_NUMBER", "RANK", "DENSE_RANK", "LEAD", "LAG", "COUNT",
            "SUM", "AVG", "MIN", "MAX", "WITH", "RECURSIVE", "TRUE", "FALSE", "TRUNCATE",
            "BEGIN", "TRANSACTION", "COMMIT", "ROLLBACK", "SAVEPOINT", "RETURNING", "FETCH", "FIRST", "ROWS", "ONLY"
        };

        public static string Format(string sql, SqlFormatRequest req)
        {
            if (string.IsNullOrWhiteSpace(sql)) return string.Empty;

            var indentStr = req.UseTabs ? "\t" : new string(' ', Math.Max(2, req.IndentSize));
            var text = sql.Trim();

            // Handle multi-query separation
            var statements = SplitStatements(text);
            var formattedStatements = new List<string>();

            foreach (var stmt in statements)
            {
                if (string.IsNullOrWhiteSpace(stmt)) continue;
                formattedStatements.Add(FormatSingleStatement(stmt, req, indentStr));
            }

            var separator = new string('\n', Math.Max(1, req.LinesBetweenQueries) + 1);
            return string.Join(separator, formattedStatements);
        }

        private static List<string> SplitStatements(string sql)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                if (c == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    sb.Append(c);
                }
                else if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    sb.Append(c);
                }
                else if (c == ';' && !inSingleQuote && !inDoubleQuote)
                {
                    sb.Append(';');
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0 && !string.IsNullOrWhiteSpace(sb.ToString()))
            {
                result.Add(sb.ToString().Trim());
            }

            return result;
        }

        private static string FormatSingleStatement(string stmt, SqlFormatRequest req, string indentStr)
        {
            // Normalize single/multiline spaces while preserving string literals
            var tokens = Tokenize(stmt);
            var sb = new StringBuilder();
            int indentLevel = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var upper = token.ToUpperInvariant();

                // Apply casing
                if (IsKeyword(token))
                {
                    token = req.KeywordCase == "lower" ? token.ToLowerInvariant() :
                            req.KeywordCase == "preserve" ? token : upper;
                }

                // Major clause breaks
                if (IsMajorClause(upper))
                {
                    if (sb.Length > 0 && !sb.ToString().EndsWith("\n"))
                    {
                        sb.AppendLine();
                    }
                    indentLevel = Math.Max(0, indentLevel);
                    sb.Append(GetIndent(indentLevel, indentStr));
                    sb.Append(token);
                    sb.Append(' ');
                    continue;
                }

                if (upper == "AND" || upper == "OR")
                {
                    sb.AppendLine();
                    sb.Append(GetIndent(indentLevel + 1, indentStr));
                    sb.Append(token);
                    sb.Append(' ');
                    continue;
                }

                if (token == ".")
                {
                    sb.Append('.');
                    continue;
                }

                if (token == ",")
                {
                    sb.Append(',');
                    sb.AppendLine();
                    sb.Append(GetIndent(indentLevel + 1, indentStr));
                    continue;
                }

                if (token == "(")
                {
                    sb.Append(" (");
                    indentLevel++;
                    continue;
                }

                if (token == ")")
                {
                    indentLevel = Math.Max(0, indentLevel - 1);
                    sb.Append(')');
                    continue;
                }

                if (token == ";")
                {
                    sb.Append(';');
                    continue;
                }

                if (sb.Length > 0 && !sb.ToString().EndsWith(" ") && !sb.ToString().EndsWith("\n") && !sb.ToString().EndsWith("(") && !sb.ToString().EndsWith("."))
                {
                    sb.Append(' ');
                }

                sb.Append(token);
            }

            return sb.ToString().Trim();
        }

        private static bool IsKeyword(string token)
        {
            var normalized = Regex.Replace(token, @"\s+", " ");
            return AllKeywords.Contains(normalized) || MajorKeywords.Contains(normalized);
        }

        private static bool IsMajorClause(string upper)
        {
            var normalized = Regex.Replace(upper, @"\s+", " ");
            return MajorKeywords.Contains(normalized);
        }

        private static string GetIndent(int level, string indentStr)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < level; i++) sb.Append(indentStr);
            return sb.ToString();
        }

        private static List<string> Tokenize(string sql)
        {
            var tokens = new List<string>();
            var pattern = @"'([^']|'')*'|""([^""]|"""")*""|--[^\r\n]*|/\*[\s\S]*?\*/|LEFT\s+JOIN|RIGHT\s+JOIN|INNER\s+JOIN|FULL\s+JOIN|CROSS\s+JOIN|GROUP\s+BY|ORDER\s+BY|INSERT\s+INTO|DELETE\s+FROM|UNION\s+ALL|CREATE\s+TABLE|ALTER\s+TABLE|DROP\s+TABLE|BEGIN\s+TRANSACTION|WITH\s+RECURSIVE|!=|<>|<=|>=|:=|[a-zA-Z_][a-zA-Z0-9_]*|[0-9]+(?:\.[0-9]+)?|[,\(\);\.\=<>!\+\-\*/]|[\S]";

            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                var val = m.Value.Trim();
                if (!string.IsNullOrEmpty(val))
                {
                    val = Regex.Replace(val, @"\s+", " ");
                    tokens.Add(val);
                }
            }

            return tokens;
        }

        public static string Minify(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return string.Empty;

            // Remove single-line comments
            var noSingleComments = Regex.Replace(sql, @"--[^\r\n]*", "");
            // Remove multi-line comments
            var noMultiComments = Regex.Replace(noSingleComments, @"/\*[\s\S]*?\*/", "");
            // Replace multiple whitespaces/newlines with single space
            var condensed = Regex.Replace(noMultiComments, @"\s+", " ").Trim();
            // Remove spaces around commas and brackets
            condensed = Regex.Replace(condensed, @"\s*([,\(\);])\s*", "$1");
            return condensed;
        }

        public static (bool IsValid, string? Error, int? LineNumber) Validate(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return (false, "Query SQL kosong", 1);

            int openParens = 0;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            int currentLine = 1;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                if (c == '\n') currentLine++;

                if (c == '\'' && !inDoubleQuote)
                {
                    if (i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i++; // Escaped quote
                    }
                    else
                    {
                        inSingleQuote = !inSingleQuote;
                    }
                }
                else if (c == '"' && !inSingleQuote)
                {
                    if (i + 1 < sql.Length && sql[i + 1] == '"')
                    {
                        i++;
                    }
                    else
                    {
                        inDoubleQuote = !inDoubleQuote;
                    }
                }
                else if (!inSingleQuote && !inDoubleQuote)
                {
                    if (c == '(') openParens++;
                    else if (c == ')')
                    {
                        openParens--;
                        if (openParens < 0)
                        {
                            return (false, $"Tanda kurung tutup ')' berlebih pada baris {currentLine}", currentLine);
                        }
                    }
                }
            }

            if (inSingleQuote) return (false, "Tanda kutip tunggal (') tidak ditutup secara lengkap", currentLine);
            if (inDoubleQuote) return (false, "Tanda kutip ganda (\") tidak ditutup secara lengkap", currentLine);
            if (openParens > 0) return (false, $"Terdapat {openParens} tanda kurung buka '(' yang belum ditutup", currentLine);

            return (true, null, null);
        }
    }
}
