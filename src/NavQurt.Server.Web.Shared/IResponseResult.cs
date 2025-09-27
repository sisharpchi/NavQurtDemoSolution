namespace NavQurt.Server.Web.Shared
{
    public interface IResponseResult
    {
        bool Success { get; }
        string? Error { get; init; }
    }

    public interface IPagedResponseResult<T>
    {
        public IEnumerable<T> Items { get; init; }
        public int ItemsCount { get; init; }
        public int PagesCount { get; init; }
    }
}
