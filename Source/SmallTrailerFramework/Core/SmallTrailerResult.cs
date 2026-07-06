namespace SmallTrailerFramework
{
    public struct SmallTrailerResult
    {
        public bool Accepted;
        public string Reason;

        public static SmallTrailerResult Success => new SmallTrailerResult { Accepted = true };

        public static SmallTrailerResult Fail(string reason)
        {
            return new SmallTrailerResult { Accepted = false, Reason = reason };
        }

        public override string ToString()
        {
            return Accepted ? "Accepted" : Reason ?? "Rejected";
        }
    }
}
