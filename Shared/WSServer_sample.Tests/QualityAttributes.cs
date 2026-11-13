using System;
using NUnit.Framework;

namespace WSServer_sample.Tests
{
    /// <summary>検証：期待値の根拠（仕様の ID・業務ルール）。消すと「期待値の変更」になる。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class BasisAttribute : PropertyAttribute
    {
        /// <param name="source">根拠（例 "EV-ORD-101"・"DEC-ORD-07"）</param>
        public BasisAttribute(string source) : base("Basis", source) { }
    }

    /// <summary>観測：期待値が、今の振る舞いを写したもの（根拠が無い）。理由を書く。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ObservationAttribute : PropertyAttribute
    {
        /// <param name="reason">観測にした理由</param>
        public ObservationAttribute(string reason) : base("Observation", reason) { }
    }
}
