using Microsoft.Data.SqlClient;
using System.Text;

namespace SQLServerInteraction
{
    public class QueryBuilder
    {
        private readonly StringBuilder _query;
        private readonly List<SQLParameter> _parameters;
        private readonly List<QueryBuilder> _subqueries;
        private readonly Stack<string> _nestedConditions;
        private bool _isUnion;
        private bool _isIntersect;
        private bool _isExcept;
        private bool _usePagination;
        private int _pageNumber;
        private int _pageSize;
        private bool _useCaseStatement;
        private string _caseStatement;

        public QueryBuilder()
        {
            _query = new StringBuilder();
            _parameters = new List<SQLParameter>();
            _subqueries = new List<QueryBuilder>();
            _nestedConditions = new Stack<string>();
            _isUnion = false;
            _isIntersect = false;
            _isExcept = false;
            _usePagination = false;
            _pageNumber = 1;
            _pageSize = 10;
            _useCaseStatement = false;
            _caseStatement = string.Empty;
        }

        public void Select(string columns = "")
        {
            _query.Append($"SELECT {columns} ");
        }

        public void From(string tableName)
        {
            _query.Append($"FROM {tableName} ");
        }

        public void Where(string condition)
        {
            _query.Append($"WHERE {condition} ");
        }

        public void And(string condition)
        {
            _query.Append($"AND {condition} ");
        }

        public void Or(string condition)
        {
            _query.Append($"OR {condition} ");
        }

        public void AddParameter(string parameterName, object value)
        {
            SQLParameter parameter = new SQLParameter { Name = parameterName, Value = value };
            _parameters.Add(parameter);
        }

        public void Join(string tableName, string onCondition, JoinType joinType = JoinType.Inner)
        {
            _query.Append($"{joinType.ToString().ToUpper()} JOIN {tableName} ON {onCondition} ");
        }

        public void OrderBy(string columns, SortOrder sortOrder = SortOrder.Ascending)
        {
            _query.Append($"ORDER BY {columns} {sortOrder.ToString().ToUpper()} ");
        }

        public void GroupBy(string columns)
        {
            _query.Append($"GROUP BY {columns} ");
        }

        public void Count(string columnName, string alias)
        {
            _query.Append($"COUNT({columnName}) AS {alias}");
        }

        public void Sum(string columnName, string alias)
        {
            _query.Append($"SUM({columnName}) AS {alias}");
        }

        public void Avg(string columnName, string alias)
        {
            _query.Append($"AVG({columnName}) AS {alias}");
        }

        public void Min(string columnName, string alias)
        {
            _query.Append($"MIN({columnName}) AS {alias}");
        }

        public void Max(string columnName, string alias)
        {
            _query.Append($"MAX({columnName}) AS {alias}" );
        }

        public QueryBuilder CreateSubquery()
        {
            var subquery = new QueryBuilder();
            _subqueries.Add(subquery);
            return subquery;
        }

        public void StartNestedCondition()
        {
            _nestedConditions.Push("(");
        }

        public void EndNestedCondition()
        {
            if (_nestedConditions.Count == 0)
                throw new InvalidOperationException("No nested condition to end.");

            var nestedCondition = _nestedConditions.Pop();
            _query.Append(nestedCondition);
            _query.Append(") ");
        }

        public void Paginate(int page, int pageSize)
        {
            _usePagination = true;
            _pageNumber = page;
            _pageSize = pageSize;
        }

        public void StartCaseStatement(string columnName)
        {
            _useCaseStatement = true;
            _caseStatement = $"CASE {columnName} ";
        }

        public void AddCaseWhen(string condition, string result)
        {
            if (!_useCaseStatement)
                throw new InvalidOperationException("CASE statement not started. Call StartCaseStatement first.");

            _caseStatement += $"WHEN {condition} THEN {result} ";
        }

        public void AddCaseElse(string result)
        {
            if (!_useCaseStatement)
                throw new InvalidOperationException("CASE statement not started. Call StartCaseStatement first.");

            _caseStatement += $"ELSE {result} ";
        }

        public void EndCaseStatement(string alias)
        {
            if (!_useCaseStatement)
                throw new InvalidOperationException("CASE statement not started. Call StartCaseStatement first.");

            _caseStatement += $"END AS {alias} ";
            _useCaseStatement = false;
            _query.Append(_caseStatement);
            _caseStatement = string.Empty;
        }

        public void Union()
        {
            _isUnion = true;
        }

        public void Intersect()
        {
            _isIntersect = true;
        }

        public void Except()
        {
            _isExcept = true;
        }

        public QueryBuildResult Build()
        {
            if (!_query.ToString().ToUpper().Contains("SELECT") || !_query.ToString().ToUpper().Contains("FROM"))
                throw new InvalidOperationException("A valid SELECT statement with FROM clause is required.");

            var formattedParameters = new StringBuilder();
            foreach (var parameter in _parameters)
            {
                formattedParameters.Append($"@{parameter.Name}, ");
            }

            var parametersString = formattedParameters.ToString().TrimEnd(' ', ',');

            foreach (var subquery in _subqueries)
            {
                _query.Append($"({subquery.Build()}) ");
            }

            if (_usePagination)
            {
                _query.Append($"OFFSET {(_pageNumber - 1) * _pageSize} ROWS FETCH NEXT {_pageSize} ROWS ONLY ");
            }

            while (_nestedConditions.Count > 0)
            {
                var nestedCondition = _nestedConditions.Pop();
                _query.Append(nestedCondition);
                _query.Append(") ");
            }

            if (_isUnion)
                _query.Insert(0, "UNION ");
            else if (_isIntersect)
                _query.Insert(0, "INTERSECT ");
            else if (_isExcept)
                _query.Insert(0, "EXCEPT ");

            var queryString = _query.ToString();

            return new QueryBuildResult
            {
                SQL = queryString,
                Parameters = parametersString
            };
        }

        private class SQLParameter
        {
            public string? Name { get; set; }
            public object? Value { get; set; }
        }
    }

    public class QueryBuildResult
    {
        public string? SQL { get; set; }
        public string? Parameters { get; set; }
    }

    public enum JoinType
    {
        Inner,
        Left,
        Right,
        Full
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }
}
