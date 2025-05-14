using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace OrderFoodPos.Repositories
{
	public abstract class BaseRepository<T> where T : class
	{
		protected readonly IDbConnection _dbConnection;

		public BaseRepository(IDbConnection dbConnection)
		{
			_dbConnection = dbConnection;
		}

		/// <summary>
		/// 通用的新增資料方法，返回受影響的行數
		/// </summary>
		public virtual async Task<int> AddAsync(string insertQuery, object parameters)
		{
			return await _dbConnection.ExecuteAsync(insertQuery, parameters);
		}

		/// <summary>
		/// 通用的 ExecuteScalar 方法，返回單一值（泛型支援）
		/// </summary>
		public virtual async Task<TResult> ExecuteScalarAsync<TResult>(string query, object parameters = null)
		{
			return await _dbConnection.ExecuteScalarAsync<TResult>(query, parameters);
		}

		/// <summary>
		/// 通用的取得單筆資料方法（T 型別）
		/// </summary>
		public virtual async Task<T?> GetAsync(string selectQuery, object parameters)
		{
			return await _dbConnection.QueryFirstOrDefaultAsync<T>(selectQuery, parameters);
		}

		/// <summary>
		/// 通用的取得單筆資料方法（支援自定義型別）
		/// </summary>
		public virtual async Task<TResult?> GetAsync<TResult>(string selectQuery, object parameters)
		{
			return await _dbConnection.QueryFirstOrDefaultAsync<TResult>(selectQuery, parameters);
		}

		/// <summary>
		/// 通用的取得多筆資料方法（T 型別）
		/// </summary>
		public virtual async Task<IEnumerable<T>> GetAllAsync(string selectQuery, object parameters = null)
		{
			return await _dbConnection.QueryAsync<T>(selectQuery, parameters);
		}

		/// <summary>
		/// 通用的取得多筆資料方法（支援自定義型別）
		/// </summary>
		public virtual async Task<IEnumerable<TResult>> GetAllAsync<TResult>(string selectQuery, object parameters = null)
		{
			return await _dbConnection.QueryAsync<TResult>(selectQuery, parameters);
		}

		/// <summary>
		/// 通用的更新資料方法，返回受影響的行數
		/// </summary>
		public virtual async Task<int> UpdateAsync(string updateQuery, object parameters)
		{
			return await _dbConnection.ExecuteAsync(updateQuery, parameters);
		}

		/// <summary>
		/// 通用的刪除資料方法，返回受影響的行數
		/// </summary>
		public virtual async Task<int> DeleteAsync(string deleteQuery, object parameters)
		{
			return await _dbConnection.ExecuteAsync(deleteQuery, parameters);
		}

		/// <summary>
		/// 通用的執行任意 SQL 查詢方法，返回受影響的行數
		/// </summary>
		public virtual async Task<int> ExecuteAsync(string query, object parameters = null)
		{
			return await _dbConnection.ExecuteAsync(query, parameters);
		}

		/// <summary>
		/// 開啟一個交易 (不影響舊有方法)
		/// </summary>
		public async Task<IDbTransaction> BeginTransactionAsync()
		{
			if (_dbConnection.State != ConnectionState.Open)
			{
				await Task.Run(() => _dbConnection.Open()); // 確保連線是開啟的
			}
			return _dbConnection.BeginTransaction();
		}


	}
}
