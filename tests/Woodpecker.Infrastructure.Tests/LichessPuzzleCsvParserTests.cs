using System.Text;
using FluentAssertions;
using Woodpecker.Application.Abstractions;
using Woodpecker.Infrastructure.Puzzles;

namespace Woodpecker.Infrastructure.Tests;

public class LichessPuzzleCsvParserTests
{
    private const string SampleCsv =
        "PuzzleId,FEN,Moves,Rating,RatingDeviation,Popularity,NbPlays,Themes,GameUrl,OpeningTags\n" +
        "00008,r6k/pp2r2p/4Rp1Q/3p4/8/1N1P2R1/PqP2bPP/7K b - - 0 24,f2g3 e6e7 b2b1 b3c1 b1c1 h6c1,1913,75,94,4852,advantage middlegame short,https://lichess.org/787zsVup/black#48,\n" +
        "0000D,5rk1/p4ppp/2p2n2/1p6/3P1qb1/2NB4/PPP2Q1P/1K3R2 w - - 2 21,f2g3 e6e7,1200,80,90,1000,mateIn1 short,https://lichess.org/n8Gy9RC1#41,\n" +
        "0000E,r2q1rk1/pp2b1pp/2p2n2/3p4/3P4/2NBP3/PPQ2PPP/R4RK1 w - - 0 14,e3e4 d5e4,1800,80,90,1000,endgame long,https://lichess.org/abc#41,\n";

    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Parse_WithNoFilter_ReturnsAllRows()
    {
        var parser = new LichessPuzzleCsvParser();

        var items = parser.Parse(ToStream(SampleCsv), new LichessPuzzleImportFilter(null, null, null));

        items.Should().HaveCount(3);
        items[0].SolutionMoves.Should().BeEquivalentTo(new[] { "f2g3", "e6e7", "b2b1", "b3c1", "b1c1", "h6c1" });
    }

    [Fact]
    public void Parse_WithRatingRange_FiltersOutOfRangeRows()
    {
        var parser = new LichessPuzzleCsvParser();

        var items = parser.Parse(ToStream(SampleCsv), new LichessPuzzleImportFilter(1300, 1900, null));

        items.Should().ContainSingle();
        items[0].Rating.Should().Be(1800);
    }

    [Fact]
    public void Parse_WithTheme_FiltersRowsWithoutTheme()
    {
        var parser = new LichessPuzzleCsvParser();

        var items = parser.Parse(ToStream(SampleCsv), new LichessPuzzleImportFilter(null, null, "mateIn1"));

        items.Should().ContainSingle();
        items[0].Rating.Should().Be(1200);
    }
}
