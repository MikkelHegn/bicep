using System;
using System.Text.RegularExpressions;

namespace Bicep.Core.Parser
{
    public class TextSpan
    {
        private static readonly Regex TextSpanPattern = new Regex(@"^\[(?<startInclusive>\d+)\:(?<endExclusive>\d+)\]$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public TextSpan(int position, int length) 
        {
            if (position < 0)
            {
                throw new ArgumentException("Position must not be negative.", nameof(position));
            }

            if (length < 0)
            {
                throw new ArgumentException("Length must not be negative.", nameof(length));
            }

            Position = position;
            Length = length;
        }

        public int Position { get; }

        public int Length { get; }

        public override string ToString() => $"[{Position}:{Position + Length}]";

        /// <summary>
        /// Calculates the span from the beginning of the first span to the end of the second span.
        /// </summary>
        /// <param name="a">The first span</param>
        /// <param name="b">The second span</param>
        /// <returns>the span from the beginning of the first span to the end of the second span</returns>
        public static TextSpan Between(TextSpan a, TextSpan b)
        {
            if (IsPairInOrder(a, b))
            {
                return new TextSpan(a.Position, b.Position + b.Length - a.Position);
            }

            // the spans are in reverse order - flip them
            return TextSpan.Between(b, a);
        }

        /// <summary>
        /// Calculates the span from the beginning of the first object to the end of the 2nd one.
        /// </summary>
        /// <param name="a">The first object</param>
        /// <param name="b">The second object</param>
        /// <returns>the span from the beginning of the first object to the end of the 2nd one</returns>
        public static TextSpan Between(IPositionable a, IPositionable b) => TextSpan.Between(a.Span,b.Span);

        /// <summary>
        /// Calculates the span from the end of the first span to the beginning of the second span.
        /// </summary>
        /// <param name="a">The first span</param>
        /// <param name="b">The second span</param>
        /// <returns>the span from the end of the first span to the beginning of the second span</returns>
        public static TextSpan BetweenExclusive(TextSpan a, TextSpan b)
        {
            if (IsPairInOrder(a, b))
            {
                return new TextSpan(a.Position + a.Length, b.Position - (a.Position + a.Length));
            }

            return TextSpan.BetweenExclusive(b, a);
        }

        /// <summary>
        /// Calculates the span from the end of the first object to the beginning of the second one.
        /// </summary>
        /// <param name="a">The first span</param>
        /// <param name="b">The second span</param>
        /// <returns>the span from the end of the first object to the beginning of the second one</returns>
        public static TextSpan BetweenExclusive(IPositionable a, IPositionable b) => TextSpan.BetweenExclusive(a.Span, b.Span);

        public static TextSpan BetweenInclusiveAndExclusive(IPositionable inclusive, IPositionable exclusive) => TextSpan.BetweenInclusiveAndExclusive(inclusive.Span, exclusive.Span);

        public static TextSpan BetweenInclusiveAndExclusive(TextSpan inclusive, TextSpan exclusive)
        {
            if (IsPairInOrder(inclusive, exclusive))
            {
                return new TextSpan(inclusive.Position, exclusive.Position - inclusive.Position);
            }

            // this operation is not commutative, so we can't just call ourselves with flipped order
            int startPosition = exclusive.Position + exclusive.Length;
            return new TextSpan(startPosition, inclusive.Position + inclusive.Length - startPosition);
        }


        /// <summary>
        /// Checks if the two spans are overlapping.
        /// </summary>
        /// <param name="a">The first span</param>
        /// <param name="b">The second span</param>
        public static bool AreOverlapping(IPositionable a, IPositionable b) => TextSpan.AreOverlapping(a.Span, b.Span);

        /// <summary>
        /// Checks if the two spans are overlapping.
        /// </summary>
        /// <param name="a">The first span</param>
        /// <param name="b">The second span</param>
        public static bool AreOverlapping(TextSpan a, TextSpan b)
        {
            if (a.Length == 0 || b.Length == 0)
            {
                // 0-length spans do not overlap with anything regardless of order
                // in other words, you can have an infinite number of 0-length spans at any position
                return false;
            }

            if (IsPairInOrder(a, b))
            {
                return b.Position >= a.Position && b.Position < a.Position + a.Length;
            }

            return AreOverlapping(b, a);
        }

        public static TextSpan Parse(string text)
        {
            if (TryParse(text, out TextSpan? span))
            {
                return span!;
            }

            throw new FormatException($"The specified text span string '{text}' is not valid.");
        }

        public static bool TryParse(string? text, out TextSpan? span)
        {
            span = null;

            if (text == null)
            {
                return false;
            }

            var match = TextSpanPattern.Match(text);
            if (match.Success == false)
            {
                return false;
            }

            if (int.TryParse(match.Groups["startInclusive"].Value, out int startInclusive) == false)
            {
                return false;
            }

            if (int.TryParse(match.Groups["endExclusive"].Value, out int endExclusive) == false)
            {
                return false;
            }

            int length = endExclusive - startInclusive;
            if (length < 0)
            {
                return false;
            }

            span = new TextSpan(startInclusive, length);
            return true;
        }

        /// <summary>
        /// Checks if a comes before b.
        /// </summary>
        /// <param name="a">The first span</param>
        /// <param name="b">The second span</param>
        private static bool IsPairInOrder(TextSpan a, TextSpan b) => a.Position <= b.Position;
    }
}