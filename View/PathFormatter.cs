namespace easySave_BMT.View_
{
    public static class PathFormatter
    {
        public static string Rectify(string path)
        {
            if (path != "0" && path.Length >= 1)
            {
                path += (path.EndsWith("/") || path.EndsWith("\\")) ? "" : "\\";
                path = path.Replace("/", "\\");
            }
            return path.ToLower();
        }
    }
}
