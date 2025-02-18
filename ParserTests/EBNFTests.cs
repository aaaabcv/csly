﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using expressionparser;
using indented;
using jsonparser;
using jsonparser.JsonModel;
using simpleExpressionParser;
using sly.buildresult;
using sly.lexer;
using sly.parser;
using sly.parser.generator;
using sly.parser.llparser;
using sly.parser.parser;
using sly.parser.syntax.grammar;
using Xunit;
using String = System.String;

namespace ParserTests
{
    
    [Lexer(IgnoreWS = true, IgnoreEOL = true)]
    public enum DoNotIgnoreCommentsToken
    {
        [MultiLineComment("/*","*/",true)]
        COMMENT = 1,

        [Lexeme(GenericToken.Identifier, IdentifierType.AlphaNumeric)]
        ID = 2
    }


    public interface DoNotIgnore
    {
        
    }

    public class IdentifierList : DoNotIgnore
    {

        public List<DoNotIgnoreCommentIdentifier> Ids;
        public IdentifierList(List<DoNotIgnore> ids)
        {
            Ids = ids.Cast<DoNotIgnoreCommentIdentifier>().ToList();
        }
    }
    
    public class DoNotIgnoreCommentIdentifier : DoNotIgnore
    {
        public string Name;

        public string Comment;

        public bool IsCommented => !string.IsNullOrEmpty(Comment);

        public DoNotIgnoreCommentIdentifier(string name)
        {
            Name = name;
        }

        public DoNotIgnoreCommentIdentifier(string name, string comment) : this(name)
        {
            Comment = comment;
        }
    }
    
    public class DoNotIgnoreCommentsParser
    {
        [Production("main : id *")]
        public DoNotIgnore Main(List<DoNotIgnore> ids)
        {
            return new IdentifierList(ids);
        }
        
        [Production("id : ID")]
        public DoNotIgnore SimpleId(Token<DoNotIgnoreCommentsToken> token)
        {
            return new DoNotIgnoreCommentIdentifier(token.Value);
        } 
        
        [Production("id : COMMENT ID")]
        public DoNotIgnore CommentedId(Token<DoNotIgnoreCommentsToken> comment, Token<DoNotIgnoreCommentsToken> token)
        {
            return new DoNotIgnoreCommentIdentifier(token.Value, comment.Value);
        } 
    }
    
    public static class ListExtensions
    {
        public static bool ContainsAll<IN>(this IEnumerable<IN> list1, IEnumerable<IN> list2)
        {
            return list1.Intersect(list2).Count() == list1.Count();
        }
    }

    public enum OptionTestToken
    {
        [Lexeme("a")] a = 1,
        [Lexeme("b")] b = 2,
        [Lexeme("c")] c = 3,
        [Lexeme("e")] e = 4,
        [Lexeme("f")] f = 5,

        [Lexeme("[ \\t]+", true)] WS = 100,
        [Lexeme("\\n\\r]+", true, true)] EOF = 101
    }

    public enum GroupTestToken
    {
        [Lexeme("a")] A = 1,
        [Lexeme(",")] COMMA = 2,
        [Lexeme(";")] SEMICOLON = 3,


        [Lexeme("[ \\t]+", true)] WS = 100,
        [Lexeme("\\n\\r]+", true, true)] EOL = 101,
        EOF = 0
    }

    public class OptionTestParser
    {
        [Production("root2 : a B? c ")]
        public string Root2(Token<OptionTestToken> a, ValueOption<string> b, Token<OptionTestToken> c)
        {
            var r = new StringBuilder();
            r.Append("R(");
            r.Append(a.Value);
            r.Append(b.Match(v => $",{v}", () => ",<none>"));
            r.Append($",{c.Value}");
            r.Append(")");
            return r.ToString();
        }

        [Production("root3 : a B c? ")]
        public string root(Token<OptionTestToken> a, string b, Token<OptionTestToken> c)
        {
            var r = $"R({a.StringWithoutQuotes},{b}";
            if (c.IsEmpty)
                r = $"{r},<none>)";
            else
                r = $"{r},{c.Value})";
            return r;
        }

        [Production("root : a b? c ")]
        public string root(Token<OptionTestToken> a, Token<OptionTestToken> b, Token<OptionTestToken> c)
        {
            var result = new StringBuilder();
            result.Append("R(");
            result.Append(a.StringWithoutQuotes);
            result.Append(",");
            if (b.IsEmpty)
                result.Append("<none>");
            else
                result.Append(b.StringWithoutQuotes);
            result.Append(",");
            result.Append(c.StringWithoutQuotes);
            result.Append(")");

            return result.ToString();
        }


        [Production("B : b ")]
        public string bee(Token<OptionTestToken> b)
        {
            return $"B({b.Value})";
        }
    }

    public class GroupTestParser
    {
        [Production("rootGroup : A ( COMMA A ) ")]
        public string root(Token<GroupTestToken> a, Group<GroupTestToken, string> group)
        {
            var r = new StringBuilder();
            r.Append("R(");
            r.Append(a.Value);
            r.Append("; {");
            group.Items.ForEach(item =>
            {
                r.Append(",");
                r.Append(item.IsValue ? item.Value : item.Token.Value);
            });
            r.Append("}");
            r.Append(")");
            return r.ToString();
        }
        
        [Production("rootGroupChoice : A ( [SEMICOLON | COMMA ] A ) ")]
        public string rootGroupChoice(Token<GroupTestToken> a, Group<GroupTestToken, string> group)
        {
            var r = new StringBuilder();
            r.Append("R(");
            r.Append(a.Value);
            r.Append(",").Append(group.Token(1).Value).Append(")");
            return r.ToString();
        }
        [Production("rootGroupChoiceMany : A ( [SEMICOLON | COMMA ] A )* ")]
        public string rootGroupChoiceMany(Token<GroupTestToken> a, List<Group<GroupTestToken, string>> groups)
        {
            var r = new StringBuilder();
            r.Append("R(");
            r.Append(a.Value);
            groups.ForEach(group =>
            {
                r.Append(",").Append(group.Token(1).Value);
            });
            r.Append(")");
            return r.ToString();
        }

        [Production("rootMany : A ( COMMA [d] A )* ")]
        public string rootMany(Token<GroupTestToken> a, List<Group<GroupTestToken, string>> groups)
        {
            var r = new StringBuilder();
            r.Append("R(");
            r.Append(a.Value);
            groups.ForEach(group =>
            {
                group.Items.ForEach(item =>
                {
                    r.Append(",");
                    r.Append(item.Match(
                        (name, token) => token.Value,
                        (name, val) => val)
                    );
                });
            });
            r.Append(")");
            return r.ToString();
        }

        [Production("rootOption : A ( SEMICOLON [d] A )? ")]
        public string rootOption(Token<GroupTestToken> a, ValueOption<Group<GroupTestToken, string>> option)
        {
            var builder = new StringBuilder();
            builder.Append("R(");
            builder.Append(a.Value);
            option.Match(
                group =>
                {
                    var aToken = group.Token(0).Value;
                    builder.Append($";{aToken}");
                    return null;
                },
                () =>
                {
                    builder.Append($";<none>");
                    return null;
                });
            builder.Append(")");
            return builder.ToString();
        }

        [Production("root3 : A ( COMMA [d] A )* ")]
        public string root3(Token<GroupTestToken> a, List<Group<GroupTestToken, string>> groups)
        {
            var r = new StringBuilder();
            r.Append("R(");
            r.Append(a.Value);
            groups.ForEach(group =>
            {
                group.Items.ForEach(item =>
                {
                    r.Append(",");
                    r.Append(item.Value);
                });
            });
            return r.ToString();
        }
    }


    public class Bugfix100Test
    {
        [Production("testNonTerm : sub* COMMA ")]
        public int TestNonTerminal(List<int> options, Token<GroupTestToken> token)
        {
            return 1;
        }

        [Production("sub : A")]
        public int sub(Token<GroupTestToken> token)
        {
            return 1;
        }

        [Production("testTerm : A* COMMA")]
        public int TestTerminal(List<Token<GroupTestToken>> options, Token<GroupTestToken> token)
        {
            return 1;
        }
    }


    public class AlternateChoiceTestTerminal
    {
        [Production("choice : [ a | b | c]")]
        public string Choice(Token<OptionTestToken> c)
        {
            return c.Value;
        }
    }
    
    public class AlternateChoiceTestZeroOrMoreTerminal
    {
        [Production("choice : [ a | b | c]*")]
        public string Choice(List<Token<OptionTestToken>> list)
        {
            return string.Join(",",list.Select(x => x.Value));
        }
    }

    public class AlternateChoiceTestOneOrMoreTerminal
    {
        [Production("choice : [ a | b | c]+")]
        public string Choice(List<Token<OptionTestToken>> list)
        {
            return string.Join(",", list.Select(x => x.Value));
        }
    }

    public class AlternateChoiceTestOptionTerminal
    {
        [Production("choice : [ a | b | c] [ b | c]?")]
        public string Choice(Token<OptionTestToken> first, Token<OptionTestToken> next)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(first.Value);

            if (next.IsEmpty)
            {
                builder.Append($",<none>");
            }
            else
            {
                builder.Append($",{next.Value}");
            }
            
            return builder.ToString();
        }
    }
    
    public class AlternateChoiceTestOptionDiscardedTerminal
    {
        [Production("choice : [ a | b | c] [ b | c] [d]")]
        public string Choice(Token<OptionTestToken> first)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(first.Value);

            
            return builder.ToString();
        }
    }

    
    public class LeftRecWithChoiceInGroup
    {
        [Production("leftrec: ([leftrec | right])")]
        public string leftrec(Group<OptionTestToken, string> opt)
        {
            return "";
        }

        [Production("right : a")]
        public string right(Token<OptionTestToken> a)
        {
            return a.Value;
        }
    }
    
    public class AlternateChoiceInGroupTestError
    {

        [Production("choiceInGroup : a ([ a | b | C | D] a)")]
        public string ChoiceInGroup(Token<OptionTestToken> c, Group<OptionTestToken, string> opt)
        {
            return c.Value;
        }
    }

    public class AlternateChoiceTestError
    {
        [Production("choice : [ a | b | C | D]")]
        public string Choice(Token<OptionTestToken> c)
        {
            return "choice";
        }
        [Production("D : [ E | C] [d]")]
        public string D()
        {
            return "nothing here";
        }
        
        [Production("E : e")]
        public string E(Token<OptionTestToken> e)
        {
            return e.Value;
        }

        [Production("C : c")]
        public string C(Token<OptionTestToken> c)
        {
            return c.Value;
        }
        
    }
    
    public class AlternateChoiceTestNonTerminal
    {
        [Production("choice : [ A | B | C]")]
        public string Choice(string c)
        {
            return c;
        }

        [Production("C : c")]
        public string C(Token<OptionTestToken> t)
        {
            return $"C({t.Value})";
        }
        
        [Production("B : b")]
        public string B(Token<OptionTestToken> t)
        {
            return $"B({t.Value})";
        }
        
        [Production("A : a")]
        public string A(Token<OptionTestToken> t)
        {
            return $"A({t.Value})";
        }
        
    }

    public class AlternateChoiceTestOneOrMoreNonTerminal
    {
        [Production("choice : [ A | B | C]+")]
        public string Choice(List<String> choices)
        {
            return string.Join(" ", choices);
        }

        [Production("C : c")]
        public string C(Token<OptionTestToken> t)
        {
            return $"C({t.Value})";
        }

        [Production("B : b")]
        public string B(Token<OptionTestToken> t)
        {
            return $"B({t.Value})";
        }

        [Production("A : a")]
        public string A(Token<OptionTestToken> t)
        {
            return $"A({t.Value})";
        }

    }

    public class Bugfix104Test
    {
        [Production("testNonTerm : sub (COMMA[d] unreachable)? ")]
        public int TestNonTerminal(int sub, ValueOption<Group<GroupTestToken,int>> group)
        {
            return 1;
        }

        [Production("sub : A")]
        public int Sub(Token<GroupTestToken> token)
        {
            return 1;
        }


        [Production("unreachable : A")]
        public int Unreachable(Token<GroupTestToken> token)
        {
            return 1;
        }
    }

    [Lexer]
    public enum Issue190Token
    {
        EOF = 0,
        
        [Lexeme(GenericToken.Identifier,IdentifierType.Alpha)]
        ID = 1,
        
        [Lexeme(GenericToken.KeyWord,"not")]
        NOT = 2,
        
        [Lexeme(GenericToken.KeyWord,"true")]
        TRUE = 3,
        
        [Lexeme(GenericToken.KeyWord,"false")]
        FALSE = 4,
        
        [Lexeme(GenericToken.KeyWord,"yes")]
        YES = 5,
        
        [Lexeme(GenericToken.KeyWord,"no")]
        NO = 6
        
    }

    public class Issue190parser
    {
        [Production("root: NOT? [TRUE | FALSE | YES | NO]")]
        public bool BooleanValue(Token<Issue190Token> notToken, Token<Issue190Token> valueToken)
        {
            bool value = valueToken.TokenID == Issue190Token.YES || valueToken.TokenID == Issue190Token.TRUE;
            if (!notToken.IsEmpty)
            {
                value = !value;
            }

            return value;
        }

    }

    public class EBNFTests
    {
        public enum TokenType
        {
            [Lexeme("a")] a = 1,
            [Lexeme("b")] b = 2,
            [Lexeme("c")] c = 3,
            [Lexeme("e")] e = 4,
            [Lexeme("f")] f = 5,
            [Lexeme("[ \\t]+", true)] WS = 100,
            [Lexeme("\\n\\r]+", true, true)] EOL = 101
        }


        [Production("R : A B c ")]
        public string R(string A, string B, Token<TokenType> c)
        {
            var result = "R(";
            result += A + ",";
            result += B + ",";
            result += c.Value;
            result += ")";
            return result;
        }

        [Production("R : G+ ")]
        public string RManyNT(List<string> gs)
        {
            var result = "R(";
            result += gs
                .Select(g => g.ToString())
                .Aggregate((s1, s2) => s1 + "," + s2);
            result += ")";
            return result;
        }

        [Production("G : e f ")]
        public string RManyNT(Token<TokenType> e, Token<TokenType> f)
        {
            var result = $"G({e.Value},{f.Value})";
            return result;
        }

        [Production("A : a + ")]
        public string A(List<Token<TokenType>> astr)
        {
            var result = "A(";
            result += astr
                .Select(a => a.Value)
                .Aggregate((a1, a2) => a1 + ", " + a2);
            result += ")";
            return result;
        }

        [Production("B : b * ")]
        public string B(List<Token<TokenType>> bstr)
        {
            if (bstr.Any())
            {
                var result = "B(";
                result += bstr
                    .Select(b => b.Value)
                    .Aggregate((b1, b2) => b1 + ", " + b2);
                result += ")";
                return result;
            }

            return "B()";
        }

       


        private Parser<TokenType, string> Parser;

        private BuildResult<Parser<TokenType, string>> BuildParser()
        {
            var parserInstance = new EBNFTests();
            var builder = new ParserBuilder<TokenType, string>();
            var result = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "R");
            return result;
        }


        private BuildResult<Parser<JsonToken, JSon>> BuildEbnfJsonParser()
        {
            var parserInstance = new EbnfJsonParser();
            var builder = new ParserBuilder<JsonToken, JSon>();

            var result =
                builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            return result;
        }

        private BuildResult<Parser<OptionTestToken, string>> BuildOptionParser()
        {
            var parserInstance = new OptionTestParser();
            var builder = new ParserBuilder<OptionTestToken, string>();

            var result =
                builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            return result;
        }

        private BuildResult<Parser<GroupTestToken, string>> BuildGroupParser()
        {
            var parserInstance = new GroupTestParser();
            var builder = new ParserBuilder<GroupTestToken, string>();

            var result =
                builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "rootGroup");
            return result;
        }

        private void AssertString(JObject obj, string key, string value)
        {
            Assert.True(obj.ContainsKey(key));
            Assert.True(obj[key].IsValue);
            var val = (JValue) obj[key];
            Assert.True(val.IsString);
            Assert.Equal(value, val.GetValue<string>());
        }

        private void AssertInt(JObject obj, string key, int value)
        {
            Assert.True(obj.ContainsKey(key));
            Assert.True(obj[key].IsValue);
            var val = (JValue) obj[key];
            Assert.True(val.IsInt);
            Assert.Equal(value, val.GetValue<int>());
        }


        private void AssertInt(JList list, int index, int value)
        {
            Assert.True(list[index].IsValue);
            var val = (JValue) list[index];
            Assert.True(val.IsInt);
            Assert.Equal(value, val.GetValue<int>());
        }

        [Fact]
        public void TestBuildGroupParser()
        {
            var buildResult = BuildGroupParser();
            Assert.False(buildResult.IsError);
            // var optionParser = buildResult.Result;

            // var result = optionParser.Parse("a , a", "root");
            // Assert.False(result.IsError);
            // Assert.Equal("R(a,B(b),c)", result.Result);
        }

        [Fact]
        public void TestEmptyOptionalNonTerminal()
        {
            var buildResult = BuildOptionParser();
            Assert.False(buildResult.IsError);
            var optionParser = buildResult.Result;

            var result = optionParser.Parse("a c", "root2");
            Assert.False(result.IsError);
            Assert.Equal("R(a,<none>,c)", result.Result);
        }

        [Fact]
        public void TestEmptyOptionTerminalInMiddle()
        {
            var buildResult = BuildOptionParser();
            Assert.False(buildResult.IsError);
            var optionParser = buildResult.Result;

            var result = optionParser.Parse("a c", "root2");
            Assert.Equal("R(a,<none>,c)", result.Result);
        }


        [Fact]
        public void TestEmptyTerminalOption()
        {
            var buildResult = BuildOptionParser();
            Assert.False(buildResult.IsError);
            var optionParser = buildResult.Result;

            var result = optionParser.Parse("a b", "root3");
            Assert.Equal("R(a,B(b),<none>)", result.Result);
        }

        [Fact]
        public void TestErrorMissingClosingBracket()
        {
            var jsonParser = new EbnfJsonGenericParser();
            var builder = new ParserBuilder<JsonTokenGeneric, JSon>();
            var build = builder.BuildParser(jsonParser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            var parserTest = build.Result;
            ParseResult<JsonTokenGeneric, JSon> r = null;
            try
            {
                r = parserTest.Parse("{");
            }
            catch (Exception e)
            {
                var stack = e.StackTrace;
                var message = e.Message;
            }

            Assert.True(r.IsError);
        }

        [Fact]
        public void TestGroupSyntaxManyParser()
        {
            var buildResult = BuildGroupParser();
            Assert.False(buildResult.IsError);
            var groupParser = buildResult.Result;
            var res = groupParser.Parse("a ,a , a ,a,a", "rootMany");

            Assert.False(res.IsError);
            Assert.Equal("R(a,a,a,a,a)", res.Result); // rootMany
        }
        
        [Fact]
        public void TestGroupSyntaxChoicesParser()
        {
            var buildResult = BuildGroupParser();
            Assert.False(buildResult.IsError);
            var groupParser = buildResult.Result;
            var res = groupParser.Parse("a ;a ", "rootGroupChoice");

            Assert.False(res.IsError);
            Assert.Equal("R(a,a)", res.Result); 
            res = groupParser.Parse("a ,a ", "rootGroupChoice");

            Assert.False(res.IsError);
            Assert.Equal("R(a,a)", res.Result); // rootMany
        }
        
        [Fact]
        public void TestGroupSyntaxChoicesManyParser()
        {
            var buildResult = BuildGroupParser();
            Assert.False(buildResult.IsError);
            var groupParser = buildResult.Result;
            var res = groupParser.Parse("a ;a,a  ; a,a ", "rootGroupChoiceMany");

            Assert.False(res.IsError);
            Assert.Equal("R(a,a,a,a,a)", res.Result); // rootMany
        }

        [Fact]
        public void TestGroupSyntaxOptionIsSome()
        {
            var buildResult = BuildGroupParser();
            Assert.False(buildResult.IsError);
            var groupParser = buildResult.Result;
            var res = groupParser.Parse("a ; a ", "rootOption");

            Assert.False(res.IsError);
            Assert.Equal("R(a;a)", res.Result); // rootMany
        }

        [Fact]
        public void TestGroupSyntaxOptionIsNone()
        {
            var buildResult = BuildGroupParser();
            Assert.False(buildResult.IsError);
            var groupParser = buildResult.Result;
            var res = groupParser.Parse("a ", "rootOption");

            Assert.False(res.IsError);
            Assert.Equal("R(a;<none>)", res.Result); // rootMany
        }

        [Fact]
        public void TestGroupSyntaxParser()
        {
            var buildResult = BuildGroupParser();
            Assert.False(buildResult.IsError);
            var groupParser = buildResult.Result;
            var res = groupParser.Parse("a ,a");

            Assert.False(res.IsError);
            Assert.Equal("R(a; {,,,a})", res.Result);
        }


        [Fact]
        public void TestJsonList()
        {
            var buildResult = BuildEbnfJsonParser();
            Assert.False(buildResult.IsError);
            var jsonParser = buildResult.Result;

            var result = jsonParser.Parse("[1,2,3,4]");
            Assert.False(result.IsError);
            Assert.True(result.Result.IsList);
            var list = (JList) result.Result;
            Assert.Equal(4, list.Count);
            AssertInt(list, 0, 1);
            AssertInt(list, 1, 2);
            AssertInt(list, 2, 3);
            AssertInt(list, 3, 4);
        }

        [Fact]
        public void TestJsonObject()
        {
            var buildResult = BuildEbnfJsonParser();
            Assert.False(buildResult.IsError);
            var jsonParser = buildResult.Result;
            var result = jsonParser.Parse("{\"one\":1,\"two\":2,\"three\":\"trois\" }");
            Assert.False(result.IsError);
            Assert.True(result.Result.IsObject);
            var o = (JObject) result.Result;
            Assert.Equal(3, o.Count);
            AssertInt(o, "one", 1);
            AssertInt(o, "two", 2);
            AssertString(o, "three", "trois");
        }

        [Fact]
        public void TestNonEmptyOptionalNonTerminal()
        {
            var buildResult = BuildOptionParser();
            Assert.False(buildResult.IsError);
            var optionParser = buildResult.Result;

            var result = optionParser.Parse("a b c", "root2");
            Assert.False(result.IsError);
            Assert.Equal("R(a,B(b),c)", result.Result);
        }


        [Fact]
        public void TestNonEmptyTerminalOption()
        {
            var buildResult = BuildOptionParser();
            Assert.False(buildResult.IsError);
            var optionParser = buildResult.Result;

            var result = optionParser.Parse("a b c", "root");
            Assert.Equal("R(a,b,c)", result.Result);
        }


        [Fact]
        public void TestOneOrMoreNonTerminal()
        {
            var buildResult = BuildParser();
            Assert.False(buildResult.IsError);
            Parser = buildResult.Result;
            var result = Parser.Parse("e f e f");
            Assert.False(result.IsError);
            Assert.Equal("R(G(e,f),G(e,f))", result.Result.Replace(" ", ""));
        }


        [Fact]
        public void TestOneOrMoreWithMany()
        {
            var buildResult = BuildParser();
            Assert.False(buildResult.IsError);
            Parser = buildResult.Result;
            var result = Parser.Parse("aaa b c");
            Assert.False(result.IsError);
            Assert.Equal("R(A(a,a,a),B(b),c)", result.Result.Replace(" ", ""));
        }

        [Fact]
        public void TestOneOrMoreWithOne()
        {
            var buildResult = BuildParser();
            Assert.False(buildResult.IsError);
            Parser = buildResult.Result;
            var result = Parser.Parse(" b c");
            Assert.True(result.IsError);
        }

        [Fact]
        public void TestParseBuild()
        {
            var buildResult = BuildParser();
            Assert.False(buildResult.IsError);
            Parser = buildResult.Result;
            Assert.Equal(typeof(EBNFRecursiveDescentSyntaxParser<TokenType, string>), Parser.SyntaxParser.GetType());
            Assert.Equal(4, Parser.Configuration.NonTerminals.Count);
            var nt = Parser.Configuration.NonTerminals["R"];
            Assert.Equal(2, nt.Rules.Count);
            nt = Parser.Configuration.NonTerminals["A"];
            Assert.Single(nt.Rules);
            var rule = nt.Rules[0];
            Assert.Single(rule.Clauses);
            Assert.IsType<OneOrMoreClause<TokenType>>(rule.Clauses[0]);
            nt = Parser.Configuration.NonTerminals["B"];
            Assert.Single(nt.Rules);
            rule = nt.Rules[0];
            Assert.Single(rule.Clauses);
            Assert.IsType<ZeroOrMoreClause<TokenType>>(rule.Clauses[0]);
        }

        [Fact]
        public void TestZeroOrMoreWithMany()
        {
            var buildResult = BuildParser();
            Assert.False(buildResult.IsError);
            Parser = buildResult.Result;
            var result = Parser.Parse("a bb c");
            Assert.False(result.IsError);
            Assert.Equal("R(A(a),B(b,b),c)", result.Result.Replace(" ", ""));
        }

        [Fact]
        public void TestZeroOrMoreWithNone()
        {
            var buildResult = BuildParser();
            Assert.False(buildResult.IsError);
            Parser = buildResult.Result;
            var result = Parser.Parse("a  c");
            Assert.False(result.IsError);
            Assert.Equal("R(A(a),B(),c)", result.Result.Replace(" ", ""));
        }

        [Fact]
        public void TestZeroOrMoreWithOne()
        {
            var buildResult = BuildParser();
            Assert.False(buildResult.IsError);
            Parser = buildResult.Result;
            var result = Parser.Parse("a b c");
            Assert.False(result.IsError);
            Assert.Equal("R(A(a),B(b),c)", result.Result.Replace(" ", ""));
        }


        #region CONTEXTS

        private BuildResult<Parser<ExpressionToken, int>> buildSimpleExpressionParserWithContext(ParserType parserType = ParserType.EBNF_LL_RECURSIVE_DESCENT)
        {
            var startingRule = $"{typeof(SimpleExpressionParserWithContext).Name}_expressions";
            var parserInstance = new SimpleExpressionParserWithContext();
            var builder = new ParserBuilder<ExpressionToken, int>();
            var parser = builder.BuildParser(parserInstance, parserType, startingRule);
            return parser;
        }

        [Fact]
        public void TestContextualParsing()
        {
            var buildResult = buildSimpleExpressionParserWithContext();

            Assert.False(buildResult.IsError);
            var parser = buildResult.Result;
            var res = parser.ParseWithContext("2 + a", new Dictionary<string, int> {{"a", 2}});
            Assert.True(res.IsOk);
            Assert.Equal(4, res.Result);
        }

        [Fact]
        public void TestContextualParsing2()
        {
            var buildResult = buildSimpleExpressionParserWithContext();

            Assert.False(buildResult.IsError);
            var parser = buildResult.Result;
            var res = parser.ParseWithContext("2 + a * b", new Dictionary<string, int> {{"a", 2}, {"b", 3}});
            Assert.True(res.IsOk);
            Assert.Equal(8, res.Result);
        }

        [Fact]
        public void TestContextualParsingWithEbnf()
        {
            var buildResult = buildSimpleExpressionParserWithContext(ParserType.EBNF_LL_RECURSIVE_DESCENT);

            Assert.False(buildResult.IsError);
            var parser = buildResult.Result;
            var res = parser.ParseWithContext("2 + a * b", new Dictionary<string, int> {{"a", 2}, {"b", 3}});
            Assert.True(res.IsOk);
            Assert.Equal(8, res.Result);
        }

        [Fact]
        public void TestBug100()
        {
            var startingRule = $"testNonTerm";
            var parserInstance = new Bugfix100Test();
            var builder = new ParserBuilder<GroupTestToken, int>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.NotNull(builtParser.Result);
            var parser = builtParser.Result;
            Assert.NotNull(parser);
            var conf = parser.Configuration;
            var expected = new List<GroupTestToken>() {GroupTestToken.A, GroupTestToken.COMMA};

            var nonTerm = conf.NonTerminals["testNonTerm"];
            Assert.NotNull(nonTerm);
            Assert.Equal(2, nonTerm.PossibleLeadingTokens.Count);
            Assert.True(nonTerm.PossibleLeadingTokens.ContainsAll(expected));

            var term = conf.NonTerminals["testTerm"];
            Assert.NotNull(term);
            Assert.Equal(2, nonTerm.PossibleLeadingTokens.Count);
            Assert.True(term.PossibleLeadingTokens.ContainsAll(expected));
        }

        #endregion

        [Fact]
        public void TestBug104()
        {
            var startingRule = $"testNonTerm";
            var parserInstance = new Bugfix104Test();
            var builder = new ParserBuilder<GroupTestToken, int>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
        }

        [Fact]
        public void TestAlternateChoiceTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestTerminal();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
            var parseResult = builtParser.Result.Parse("a", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("a",parseResult.Result);
            parseResult = builtParser.Result.Parse("b", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("b",parseResult.Result);
            parseResult = builtParser.Result.Parse("c", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("c",parseResult.Result);
            parseResult = builtParser.Result.Parse("d", "choice");
            Assert.False(parseResult.IsOk);
        }
        
        [Fact]
        public void TestAlternateChoiceNonTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestNonTerminal();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
            var parseResult = builtParser.Result.Parse("a", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("A(a)",parseResult.Result);
            parseResult = builtParser.Result.Parse("b", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("B(b)",parseResult.Result);
            parseResult = builtParser.Result.Parse("c", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("C(c)",parseResult.Result);
            parseResult = builtParser.Result.Parse("d", "choice");
            Assert.False(parseResult.IsOk);
        }

        [Fact]
        public void TestAlternateChoiceOneOrMoreNonTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestOneOrMoreNonTerminal();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
            var parseResult = builtParser.Result.Parse("a b", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("A(a) B(b)", parseResult.Result);
            parseResult = builtParser.Result.Parse("b", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("B(b)", parseResult.Result);
            parseResult = builtParser.Result.Parse("c", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("C(c)", parseResult.Result);
            parseResult = builtParser.Result.Parse("d", "choice");
            Assert.False(parseResult.IsOk);
        }

        [Fact]
        public void TestAlternateChoiceZeroOrMoreTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestZeroOrMoreTerminal();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
            var parseResult = builtParser.Result.Parse("a b c", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("a,b,c",parseResult.Result);
            parseResult = builtParser.Result.Parse("b", "choice");
            Assert.True(parseResult.IsOk);
        }

        [Fact]
        public void TestAlternateChoiceOneOrMoreTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestOneOrMoreTerminal();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
            var parseResult = builtParser.Result.Parse("a b c", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("a,b,c", parseResult.Result);
            parseResult = builtParser.Result.Parse("b", "choice");
            Assert.True(parseResult.IsOk);
        }

        [Fact]
        public void TestAlternateChoiceOptionTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestOptionTerminal();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
            var parseResult = builtParser.Result.Parse("a b", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("a,b",parseResult.Result);
            parseResult = builtParser.Result.Parse("a", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("a,<none>",parseResult.Result);
        }
        
        [Fact]
        public void TestAlternateChoiceOptionDiscardedTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestOptionDiscardedTerminal();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            Assert.False(builtParser.Errors.Any());
            var parseResult = builtParser.Result.Parse("a b", "choice");
            Assert.True(parseResult.IsOk);
            Assert.Equal("a",parseResult.Result);
            parseResult = builtParser.Result.Parse("a", "choice");
            Assert.True(parseResult.IsError);
            Assert.Single(parseResult.Errors);
            Assert.Equal(ErrorType.UnexpectedEOS,parseResult.Errors[0].ErrorType);
        }

        [Fact]
        public void TestAlternateChoiceErrorMixedTerminalAndNonTerminal()
        {
            var startingRule = $"choice";
            var parserInstance = new AlternateChoiceTestError();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.True(builtParser.IsError);
            Assert.Equal(2,builtParser.Errors.Count);
            Assert.Equal(ErrorCodes.PARSER_MIXED_CHOICES, builtParser.Errors[0].Code);
            Assert.Equal(ErrorCodes.PARSER_NON_TERMINAL_CHOICE_CANNOT_BE_DISCARDED, builtParser.Errors[1].Code);
            
        }
        
        
        
        [Fact]
        public void TestAlternateChoiceInGroupLeftRecursion()
        {
            var startingRule = $"choiceInGroup";
            var parserInstance = new LeftRecWithChoiceInGroup();
            var builder = new ParserBuilder<OptionTestToken, string>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.True(builtParser.IsError);
            Assert.Single(builtParser.Errors);
            Assert.Equal(ErrorCodes.PARSER_LEFT_RECURSIVE,builtParser.Errors.First().Code);
            // Assert.Equal(1,builtParser.Errors.Count);
            // Assert.Equal(ErrorCodes.PARSER_MIXED_CHOICES, builtParser.Errors[0].Code);

        }


        [Fact]
        public void TestIssue190()
        {
            var startingRule = $"root";
            var parserInstance = new Issue190parser();
            var builder = new ParserBuilder<Issue190Token, bool>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.False(builtParser.IsError);
            var parser = builtParser.Result;
            var parserResultNotTrue = parser.Parse("not true");
            Assert.True(parserResultNotTrue.IsOk);
            Assert.False(parserResultNotTrue.Result);
            var parserResultTrue = parser.Parse("yes");
            Assert.True(parserResultTrue.IsOk);
            Assert.True(parserResultTrue.Result);
        }

        [Fact]
        public void TestIssue193()
        {
            var builtParser = BuildParser();
            Assert.True(builtParser.IsOk);
            Assert.NotNull(builtParser.Result);
            var parser = builtParser.Result;

            var test = parser.Parse("a b");

            Assert.True(test.IsError);
            Assert.NotEmpty(test.Errors);
            var containsEOSError = test.Errors.Exists(x =>
                x is UnexpectedTokenSyntaxError<TokenType> tok && tok.UnexpectedToken.IsEOS);
            Assert.True(containsEOSError);
                
            ;

        }
        
        [Fact]
        public void TestIssue213()
        {
            var parserInstance = new DoNotIgnoreCommentsParser();
            var builder = new ParserBuilder<DoNotIgnoreCommentsToken, DoNotIgnore>();
            var builtParser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "main");
            
            Assert.True(builtParser.IsOk);
            Assert.NotNull(builtParser.Result);
            var parser = builtParser.Result;

            var test = parser.Parse("a /*commented b*/b");

            Assert.True(test.IsOk);
            Assert.NotNull(test.Result);
            Assert.IsType<IdentifierList>(test.Result);
            var list = test.Result as IdentifierList;
            Assert.Equal(2, list.Ids.Count);
            Assert.False(list.Ids[0].IsCommented);
            Assert.Equal("a",list.Ids[0].Name);
            Assert.True(list.Ids[1].IsCommented);
            Assert.Equal("b",list.Ids[1].Name);
            Assert.Equal("commented b",list.Ids[1].Comment);    
            ;

        }

        [Fact]
        public void TestIndentedParser()
        {
            var source =@"if truc == 1
    un = 1
    deux = 2
else
    trois = 3
    quatre = 4

";
            ParserBuilder<IndentedLangLexer, Ast> builder = new ParserBuilder<IndentedLangLexer, Ast>();
            var instance = new IndentedParser();
            var parserRes = builder.BuildParser(instance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            Assert.True(parserRes.IsOk);
            var parser = parserRes.Result;
            Assert.NotNull(parser);
            var parseResult = parser.Parse(source);
            Assert.True(parseResult.IsOk);
            var ast = parseResult.Result;
            Assert.NotNull(ast);
            Assert.IsAssignableFrom<Block>(ast);
            Block root = ast as Block;
            Assert.Single(root.Statements);
            Assert.IsAssignableFrom<IfThenElse>(root.Statements.First());
            IfThenElse ifthenelse = root.Statements.First() as IfThenElse;
            Assert.NotNull(ifthenelse.Cond);
            Assert.NotNull(ifthenelse.Then);
            Assert.Equal(2,ifthenelse.Then.Statements.Count);
            Assert.NotNull(ifthenelse.Else);
            Assert.Equal(2,ifthenelse.Else.Statements.Count);
        }
        
        [Fact]
        public void TestIndentedParserNestedBlocks()
        {
            var source =@"if truc == 1
  un = 1
  deux = 2
else  
  trois = 3
  quatre = 4
  if bidule ==89
     toto = 28
final = 9999
";
            ParserBuilder<IndentedLangLexer, Ast> builder = new ParserBuilder<IndentedLangLexer, Ast>();
            var instance = new IndentedParser();
            var parserRes = builder.BuildParser(instance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            Assert.True(parserRes.IsOk);
            var parser = parserRes.Result;
            Assert.NotNull(parser);
            var parseResult = parser.Parse(source);
            Assert.True(parseResult.IsOk);
            var ast = parseResult.Result;
            Assert.NotNull(ast);
            Assert.IsAssignableFrom<Block>(ast);
            Block root = ast as Block;
            Assert.Equal(2,root.Statements.Count);
            Assert.IsAssignableFrom<IfThenElse>(root.Statements.First());
            IfThenElse ifthenelse = root.Statements.First() as IfThenElse;
            Assert.NotNull(ifthenelse.Cond);
            Assert.NotNull(ifthenelse.Then);
            Assert.Equal(2,ifthenelse.Then.Statements.Count);
            Assert.NotNull(ifthenelse.Else);
            Assert.Equal(3,ifthenelse.Else.Statements.Count);
            var lastelseStatement = ifthenelse.Else.Statements.Last();
            Assert.IsAssignableFrom<IfThenElse>(lastelseStatement);
            var nestedIf = lastelseStatement as IfThenElse;
            Assert.Null(nestedIf.Else);
            Assert.NotNull(nestedIf.Then);

            var lastStatement = root.Statements.Last();
            Assert.IsAssignableFrom<Set>(lastStatement);
            var finalSet = lastStatement as Set;
            Assert.Equal("final",finalSet.Id.Name);
            Assert.Equal(9999,finalSet.Value.Value);

        }
        
        [Fact]
        public void TestIndentedParserWithEolAwareness()
        {
            var source =@"if truc == 1
    un = 1
    deux = 2
else
    trois = 3
    quatre = 4

";
            ParserBuilder<IndentedLangLexer2, Ast> builder = new ParserBuilder<IndentedLangLexer2, Ast>();
            var instance = new IndentedParser2();
            var parserRes = builder.BuildParser(instance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            Assert.True(parserRes.IsOk);
            var parser = parserRes.Result;
            Assert.NotNull(parser);
            var parseResult = parser.Parse(source);
            Assert.True(parseResult.IsOk);
            var ast = parseResult.Result;
            Assert.NotNull(ast);
            Assert.IsAssignableFrom<Block>(ast);
            Block root = ast as Block;
            Assert.Single(root.Statements);
            Assert.IsAssignableFrom<IfThenElse>(root.Statements.First());
            IfThenElse ifthenelse = root.Statements.First() as IfThenElse;
            Assert.NotNull(ifthenelse.Cond);
            Assert.NotNull(ifthenelse.Then);
            Assert.Equal(2,ifthenelse.Then.Statements.Count);
            Assert.NotNull(ifthenelse.Else);
            Assert.Equal(2,ifthenelse.Else.Statements.Count);
        }
    }
}