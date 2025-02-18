﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using csly.indentedWhileLang.compiler;
using csly.indentedWhileLang.parser;
using csly.whileLang.compiler;
using csly.whileLang.interpreter;
using csly.whileLang.model;
using csly.whileLang.parser;
using expressionparser;
using GenericLexerWithCallbacks;
using indented;
using jsonparser;
using jsonparser.JsonModel;
using ParserTests;
using ParserTests.Issue239;
using ParserTests.lexer;
using simpleExpressionParser;
using sly.lexer;
using sly.lexer.fsm;
using sly.parser;
using sly.parser.generator;
using sly.parser.syntax.grammar;
using sly.buildresult;
using sly.i18n;
using sly.parser.generator.visitor;
using sly.parser.parser;
using Xunit;

namespace ParserExample
{
    
    public enum ManyString
    {
        [Lexeme(GenericToken.String, "'", "'")]
        [Lexeme(GenericToken.String)]
        STRING
    }
    
    public enum DoubleExponent {
        [Lexeme(GenericToken.Double)]
        DOUBLE = 1,
        
        [Lexeme(GenericToken.Extension)]
        DOUBLE_EXP = 2
    }
    
    public enum TokenType
    {
        [Lexeme("a")] a = 1,
        [Lexeme("b")] b = 2,
        [Lexeme("c")] c = 3,
        [Lexeme("z")] z = 26,
        [Lexeme("r")] r = 21,
        [Lexeme("[ \\t]+", true)] WS = 100,
        [Lexeme("[\\r\\n]+", true, true)] EOL = 101
    }


    public enum CharTokens {
        [Lexeme(GenericToken.Char,"'","\\")]
//        [Lexeme(GenericToken.Char,"|","\\")]
        MyChar,

//        [Lexeme(GenericToken.Char,"|","\\")]
//        OtherChar,
//
//        [Lexeme(GenericToken.String,"'","\\")]
//        MyString
    }

    
    internal class Program
    {
        [Production("R : A b c ")]
        [Production("R : Rec b c ")]
        public static object R(List<object> args)
        {
            var result = "R(";
            result += args[0] + ",";
            result += (args[1] as Token<TokenType>).Value + ",";
            result += (args[2] as Token<TokenType>).Value;
            result += ")";
            return result;
        }

        [Production("A : a ")]
        [Production("A : z ")]
        public static object A(List<object> args)
        {
            var result = "A(";
            result += (args[0] as Token<TokenType>).Value;
            result += ")";
            return result;
        }

        [Production("Rec : r Rec ")]
        [Production("Rec :  ")]
        public static object Rec(List<object> args)
        {
            if (args.Count == 2)
            {
                var r = "Rec(" + (args[0] as Token<TokenType>).Value + "," + args[1] + ")";
                return r;
            }

            return "_";
        }


        private static void TestFactorial()
        {
            var whileParser = new WhileParserGeneric();
            var builder = new ParserBuilder<WhileTokenGeneric, WhileAST>();
            var Parser = builder.BuildParser(whileParser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "statement");

            var program = @"
(
    r:=1;
    i:=1;
    while i < 11 do 
    (";
            //program += "\nprint \"r=\".r;\n";
            program += "r := r * i;\n";
            // program += "print \"r=\".r;\n";
            // program += "print \"i=\".i;\n";
            program += "i := i + 1 \n);\n";
            program += "return r)\n";
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var result = Parser.Result.Parse(program);
                    var interpreter = new Interpreter();
                    var context = interpreter.Interprete(result.Result);

                    var compiler = new WhileCompiler();
                    var code = compiler.TranspileToCSharp(program);
                    var f = compiler.CompileToFunction(program,false);
                    int r = f();
                    if (r != 3628800)
                    {
                        throw new Exception("erreur " + r);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} : {e.StackTrace}");
                }
            }
        }
        
        private static void TestIndentedFactorial()
        {
            var whileParser = new IndentedWhileParserGeneric();
            var builder = new ParserBuilder<IndentedWhileTokenGeneric, WhileAST>();
            var Parser = builder.BuildParser(whileParser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "program");

            if (Parser.IsError)
            {
                foreach (var error in Parser.Errors)
                {
                    Console.WriteLine(error.Message);
                }

                return;
            }
            
            var program = 
@"# indented factorial
r:=1
i:=1
while i < 11 do
	print ""i="".i
	print ""r="".r
	r := r * i
	i := i + 1
print ""toto""
# begin a nested block
    x := 123
    y := 456
    z := x * y + 1
    print ""z= "".z
# end of nested block
return r";
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var result = Parser.Result.Parse(program);
                    if (result.IsError)
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine(error.ErrorMessage);
                        }

                        return;
                    }
                    
                    var interpreter = new Interpreter();
                    var context = interpreter.Interprete(result.Result);

                    var compiler = new IndentedWhileCompiler();
                    var code = compiler.TranspileToCSharp(program);
                    var f = compiler.CompileToFunction(program,false);
                    Console.WriteLine("***********************************");
                    Console.WriteLine("***********************************");
                    Console.WriteLine("***********************************");
                    int r = f();
                    if (r != 3628800)
                    {
                        throw new Exception("erreur " + r);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} : {e.StackTrace}");
                }
            }
        }

        private static void TestThreadsafeGeneric()
        {
            var whileParser = new WhileParserGeneric();
            var builder = new ParserBuilder<WhileTokenGeneric, WhileAST>();
            var Parser = builder.BuildParser(whileParser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "statement");
            var program = @"
(
    r:=1;
    i:=1;
    while i < 11 do 
    (";
            program += "r := r * i;\n";
            program += "i := i + 1 \n);\n";
            program += "return r)\n";
            for (int i = 0; i < 10; i++)
            {
                int fixed_i = i; // capture fixed i
                var t = new Thread(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            Console.WriteLine($"{fixed_i}.{j}");
                            Thread.Sleep(5);
                            Parser.Result.Parse(program);
                        }
                    }
                    catch (Exception e)
                    {
                        ;
                    }
                });
                t.Start();
                Console.WriteLine($"thread #{fixed_i} started");
            }
        }


        private static void testGenericLexerWhile()
        {
            var sw = new Stopwatch();

            var source = @"
(
    r:=1;
    i:=1;
    while i < 11 DO 
    ( 
    r := r * i;
    PRINT r;
    print i;
    i := i + 1 )
)";


            sw.Reset();
            sw.Start();
            var wpg = new WhileParserGeneric();
            var wbuilderGen = new ParserBuilder<WhileTokenGeneric, WhileAST>();
            var buildResultgen = wbuilderGen.BuildParser(wpg, ParserType.EBNF_LL_RECURSIVE_DESCENT, "statement");
            var parserGen = buildResultgen.Result;
            var rGen = parserGen.Parse(source);
            sw.Stop();
            Console.WriteLine($"generic parser : {sw.ElapsedMilliseconds} ms");
            if (!rGen.IsError)
            {
                var interpreter = new Interpreter();
                var ctx = interpreter.Interprete(rGen.Result);
            }
            else
            {
                rGen.Errors.ForEach(e => Console.WriteLine(e.ToString()));
            }
        }

        private static void testGenericLexerJson()
        {
            var sw = new Stopwatch();

            var source = File.ReadAllText("test.json");

            var wp = new EbnfJsonParser();
            sw.Reset();
            sw.Start();
            var wbuilder = new ParserBuilder<JsonToken, JSon>();
            var buildResult = wbuilder.BuildParser(wp, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            var parser = buildResult.Result;
            var r = parser.Parse(source);
            sw.Stop();
            Console.WriteLine($"json regex parser : {sw.ElapsedMilliseconds} ms");
            if (r.IsError) r.Errors.ForEach(e => Console.WriteLine(e.ToString()));


            sw.Reset();
            sw.Start();
            wbuilder = new ParserBuilder<JsonToken, JSon>();
            buildResult = wbuilder.BuildParser(wp, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            parser = buildResult.Result;
            parser.Lexer = new JSONLexer();
            r = parser.Parse(source);
            Console.WriteLine($"json hard coded lexer : {sw.ElapsedMilliseconds} ms");
            sw.Stop();


            sw.Reset();
            sw.Start();
            var wpg = new EbnfJsonGenericParser();
            var wbuilderGen = new ParserBuilder<JsonTokenGeneric, JSon>();
            var buildResultgen = wbuilderGen.BuildParser(wpg, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            var parserGen = buildResultgen.Result;
            var rGen = parserGen.Parse(source);
            sw.Stop();
            Console.WriteLine($"json generic parser : {sw.ElapsedMilliseconds} ms");
            if (rGen.IsError) rGen.Errors.ForEach(e => Console.WriteLine(e.ToString()));
        }

        private static void testJSONLexer()
        {
            var builder = new ParserBuilder<JsonToken, JSon>();
            var parser = builder.BuildParser(new JSONParser(), ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");

            var source = "{ \"k\" : 1;\"k2\" : 1.1;\"k3\" : null;\"k4\" : false}";
            //source = File.ReadAllText("test.json");
            var lexer = new JSONLexer();
            var sw = new Stopwatch();
            sw.Start();
            var lexresult = lexer.Tokenize(source);
            if (lexresult.IsOk)
            {
                var tokens = lexresult.Tokens;
                sw.Stop();
                Console.WriteLine($"hard coded lexer {tokens.Count()} tokens in {sw.ElapsedMilliseconds}ms");
                var sw2 = new Stopwatch();
                var start = DateTime.Now.Millisecond;
                sw2.Start();
                lexresult = parser.Result.Lexer.Tokenize(source);
                if (lexresult.IsOk)
                {
                    tokens = lexresult.Tokens;
                    sw2.Stop();
                    var end = DateTime.Now.Millisecond;
                    Console.WriteLine(
                        $"old lexer {tokens.Count()} tokens in {sw2.ElapsedMilliseconds}ms / {end - start}ms");
                }
            }
        }


        private static void testErrors()
        {
            var jsonParser = new JSONParser();
            var builder = new ParserBuilder<JsonToken, JSon>();
            var parser = builder.BuildParser(jsonParser, ParserType.LL_RECURSIVE_DESCENT, "root").Result;


            var source = @"{
    'one': 1,
    'bug':{,}
}".Replace("'", "\"");
            var r = parser.Parse(source);

            var isError = r.IsError; // true
            var root = r.Result; // null;
            var errors = r.Errors; // !null & count > 0
            var error = errors[0] as UnexpectedTokenSyntaxError<JsonToken>; // 
            var token = error.UnexpectedToken.TokenID; // comma
            var line = error.Line; // 3
            var column = error.Column; // 12
        }

        private static void TestRuleParser()
        {
            Console.WriteLine("hum hum...");
            var parserInstance = new RuleParser<EbnfToken>();
            var builder = new ParserBuilder<EbnfToken, IClause<EbnfToken>>();
            var r = builder.BuildParser(parserInstance, ParserType.LL_RECURSIVE_DESCENT, "rule");

            var parser = r.Result;
            var rule = parser.Parse("a ( b ) ", "clauses");
        }


        public static BuildResult<Parser<ExpressionToken, int>> buildSimpleExpressionParserWithContext()
        {


            var StartingRule = $"{typeof(SimpleExpressionParserWithContext).Name}_expressions";
            var parserInstance = new SimpleExpressionParserWithContext();
            var builder = new ParserBuilder<ExpressionToken, int>();
            var Parser = builder.BuildParser(parserInstance, ParserType.LL_RECURSIVE_DESCENT, StartingRule);
            return Parser;
        }

        public static void TestContextualParser()
        {
            var buildResult = buildSimpleExpressionParserWithContext();
            if (buildResult.IsError)
            {
                buildResult.Errors.ForEach(e =>
                {
                    Console.WriteLine(e.Level + " - " + e.Message);
                });
                return;
            }
            var parser = buildResult.Result;
            var res = parser.ParseWithContext("2 + a", new Dictionary<string, int> { { "a", 2 } });
            Console.WriteLine($"result : ok:>{res.IsOk}< value:>{res.Result}<");
        }

        public static void TestTokenCallBacks()
        {
            var res = LexerBuilder.BuildLexer(new BuildResult<ILexer<CallbackTokens>>());
            if (!res.IsError)
            {
                var lexer = res.Result as GenericLexer<CallbackTokens>;
                CallBacksBuilder.BuildCallbacks(lexer);

                var r = lexer.Tokenize("aaa bbb");
                if (r.IsOk)
                {
                    var tokens = r.Tokens;
                    foreach (var token in tokens)
                    {
                        Console.WriteLine($"{token.TokenID} - {token.Value}");
                    }
                }
            }

        }

        public static void test104()
        {
            EBNFTests tests = new EBNFTests();
            tests.TestGroupSyntaxOptionIsNone();

        }

        public static void testJSON()
        {
            try {

                var instance = new EbnfJsonGenericParser();
            var builder = new ParserBuilder<JsonTokenGeneric, JSon>();
            var buildResult = builder.BuildParser(instance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            // if (buildResult.IsOk)
            // {
            //     Console.WriteLine("parser built.");
            //     var parser = buildResult.Result;
            //     var content = File.ReadAllText("test.json");
            //     Console.WriteLine("test.json read.");
            //     var jsonResult = parser.Parse(content);
            //     Console.WriteLine("json parse done.");
            //     if (jsonResult.IsOk)
            //     {
            //         Console.WriteLine("YES !");
            //     }
            //     else
            //     {
            //         Console.WriteLine("Ooh no !");
            //     }
            //     Console.WriteLine("Done.");
            //
            // }
            // else
            // {
            //     buildResult.Errors.ForEach(e => Console.WriteLine(e.Message));
            // }
            }
            catch(Exception e) {
                Console.WriteLine($"ERROR {e.Message} : \n {e.StackTrace}");
            }

        }

        private static void TestGraphViz()
        {
            var StartingRule = $"{typeof(SimpleExpressionParser).Name}_expressions";
            var parserInstance = new SimpleExpressionParser();
            var builder = new ParserBuilder<ExpressionToken, int>();
            var parser = builder.BuildParser(parserInstance, ParserType.LL_RECURSIVE_DESCENT, StartingRule);
            var result = parser.Result.Parse("2 + 2 * 3");
            var tree = result.SyntaxTree;
            var graphviz = new GraphVizEBNFSyntaxTreeVisitor<ExpressionToken>();
            var root = graphviz.VisitTree(tree);
            string graph = graphviz.Graph.Compile();
            File.Delete("c:\\temp\\tree.dot");
            File.AppendAllText("c:\\temp\\tree.dot", graph);
        }

        private static void benchLexer()
        {
            var content = File.ReadAllText("test.json");

            var lexerRes = LexerBuilder.BuildLexer(new BuildResult<ILexer<JsonTokenGeneric>>());
            ILexer<JsonTokenGeneric> BenchedLexer = null;
            if (lexerRes != null)
            {
                BenchedLexer = lexerRes.Result;
                BenchedLexer.Tokenize(content);
            }
        }
        
        private static void TestChars()
        {
            var res = LexerBuilder.BuildLexer(new BuildResult<ILexer<CharTokens>>());
            if (res.IsOk)
            {
                var lexer = res.Result as GenericLexer<CharTokens>;

                var dump = lexer.ToString();
                var graph = lexer.ToGraphViz();
                Console.WriteLine(graph);
                var source = "'\\''";
                Console.WriteLine(source);
                var res2 = lexer.Tokenize(source);
                Console.WriteLine($"{res2.IsOk} - {res2.Tokens[0].Value}");
                var sourceU = "'\\u0066'";
                var res3 = lexer.Tokenize(sourceU);
                Console.WriteLine($"{res3.IsOk} - {res3.Tokens[0].Value}");
            }
            else
            {
                var errors = string.Join('\n',res.Errors.Select(e => e.Level + " - " + e.Message).ToList());
                Console.WriteLine("error building lexer : ");
                Console.WriteLine(errors);
            }
        }

        private static void TestGrammarParser()
        {
            string productionRule = "clauses : clause (COMMA [D] clause)*";
            var ruleparser = new RuleParser<TestGrammarToken>();
            var builder = new ParserBuilder<EbnfTokenGeneric, GrammarNode<TestGrammarToken>>();
            var grammarParser = builder.BuildParser(ruleparser, ParserType.LL_RECURSIVE_DESCENT, "rule").Result;
            var result = grammarParser.Parse(productionRule);
            //(grammarParser.Lexer as GenericLexer<TestGrammarToken>).ResetLexer();
            Console.WriteLine($"alors ? {string.Join('\n',result.Errors.Select(e => e.ErrorMessage))}");
            result = grammarParser.Parse(productionRule);
            Console.WriteLine($"alors ? {string.Join('\n',result.Errors.Select(e => e.ErrorMessage))}");
            ;
            
            Console.WriteLine("starting");
            ErroneousGrammar parserInstance = new ErroneousGrammar();
            Console.WriteLine("new instance");

            var builder2 = new ParserBuilder<TestGrammarToken, object>();
            Console.WriteLine("builder");

            var Parser = builder.BuildParser(parserInstance,ParserType.EBNF_LL_RECURSIVE_DESCENT,"rule");
            Console.WriteLine($"built : {Parser.IsOk}");

            
        }

        public static void TestRecursion()
        {
            var builder = new ParserBuilder<TestGrammarToken, object>();
            Console.WriteLine("starting");
            var  parserInstance = new RecursiveGrammar();
            Console.WriteLine("new instance");
            
            
            var Parser = builder.BuildParser(parserInstance,ParserType.EBNF_LL_RECURSIVE_DESCENT,"clause");
            Console.WriteLine($"built : {Parser.IsOk}");
            if (Parser.IsError)
            {
                foreach (var error in Parser.Errors)
                {
                    Console.WriteLine($"{error.Code} - {error.Message}");
                }
            }

            Console.WriteLine("/-----------------------------------");
            Console.WriteLine("/---");
            Console.WriteLine("/-----------------------------------");
            
             builder = new ParserBuilder<TestGrammarToken, object>();
            Console.WriteLine("starting");
            var  parserInstance2 = new RecursiveGrammar2();
            Console.WriteLine("new instance");


             Parser = builder.BuildParser(parserInstance2,ParserType.EBNF_LL_RECURSIVE_DESCENT,"clause");
            Console.WriteLine($"built : {Parser.IsOk}");
            if (Parser.IsError)
            {
                foreach (var error in Parser.Errors)
                {
                    Console.WriteLine($"{error.Code} - {error.Message}");
                }
            }
        }


        public static void TestScript()
        {
            var parserInstance = new ScriptParser();
            var builder = new ParserBuilder<ScriptToken, object>();
            var parserBuild = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "test");
            if (parserBuild.IsOk)
            {
                var parser = parserBuild.Result;
                string ko1 = "|B|test2(a, b, c=100)|E|";
                string ko2 = "|B|plotshape(data, style=shapexcross)|E|";
                
                var r = parser.Parse(ko1);
                var graphviz = new GraphVizEBNFSyntaxTreeVisitor<ScriptToken>();
                var root = graphviz.VisitTree(r.SyntaxTree);
                var graph = graphviz.Graph.Compile();
                r = parser.Parse(ko2);
            }
            else
            {
                foreach (var e in parserBuild.Errors)
                {
                    Console.WriteLine(e.Level+ " - " + e.Message);
                }
            }
        }

        public static void TestI18N()
        {
            Console.WriteLine("****************************************");
            Console.WriteLine("***");
            Console.WriteLine("***          ENGLISH ");
            Console.WriteLine("***");
            var e = I18N.Instance.GetText("en", Message.UnexpectedEos);
            Console.WriteLine(e);
            var ee = I18N.Instance.GetText("en", Message.UnexpectedToken,"xxx","SOME_TOKEN");
            Console.WriteLine(ee);
            var eee = I18N.Instance.GetText("en", Message.UnexpectedTokenExpecting,"xxx","SOME_TOKEN","OTHER_TOKEN1, OTHER_TOKEN2, OTHER_TOKEN_3");
            Console.WriteLine(eee);
            Console.WriteLine("****************************************");
            Console.WriteLine("***");
            Console.WriteLine("***          LOCAL ");
            Console.WriteLine("***");
            e = I18N.Instance.GetText( Message.UnexpectedEos);
            Console.WriteLine(e);
            ee = I18N.Instance.GetText( Message.UnexpectedToken,"xxx","SOME_TOKEN");
            Console.WriteLine(ee);
            eee = I18N.Instance.GetText( Message.UnexpectedTokenExpecting,"xxx","SOME_TOKEN","OTHER_TOKEN1, OTHER_TOKEN2, OTHER_TOKEN_3");
            Console.WriteLine(eee);
            ;
        }

        private static BuildResult<Parser<ExpressionToken, double>> BuildParserExpression()
        {   
            var StartingRule = $"{typeof(SimpleExpressionParser).Name}_expressions";
            var parserInstance = new SimpleExpressionParser();
            var builder = new ParserBuilder<ExpressionToken, double>();
            return builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, StartingRule);
        }

        
        public static void TestAssociativityFactorExpressionParser()
        {
            var StartingRule = $"{typeof(SimpleExpressionParser).Name}_expressions";
            var Parser = BuildParserExpression();
            var r = Parser.Result.Parse("1 / 2 / 3", StartingRule);
            Console.WriteLine($"{r.IsOk} : {r.Result}");
            ;
        }


        public static void TestManyString()
        {
            var lexerRes = LexerBuilder.BuildLexer(new BuildResult<ILexer<ManyString>>());
            Assert.False(lexerRes.IsError);
            var lexer = lexerRes.Result;
            var string1 = "\"hello \\\"world \"";
            var expectString1 = "\"hello \"world \"";
            var string2 = "'that''s it'";
            var expectString2 = "'that's it'";
            var source1 = $"{string1} {string2}";
            var r = lexer.Tokenize(source1);
            Assert.True(r.IsOk);
            Assert.Equal(3, r.Tokens.Count);
            var tok1 = r.Tokens[0];
            Assert.Equal(ManyString.STRING, tok1.TokenID);
            Assert.Equal(expectString1, tok1.Value);
            Assert.Equal('"',tok1.StringDelimiter);

            var tok2 = r.Tokens[1];
            Assert.Equal(ManyString.STRING, tok2.TokenID);
            Assert.Equal(expectString2, tok2.Value);
            Assert.Equal('\'',tok2.StringDelimiter);
        }

        
        private static void AddExponentExtension(DoubleExponent token, LexemeAttribute lexem, GenericLexer<DoubleExponent> lexer) {
            if (token == DoubleExponent.DOUBLE_EXP) {

   	
                // callback on end_exponent node 
                NodeCallback<GenericToken> callback = (FSMMatch<GenericToken> match) => 
                {
                    string[] items = match.Result.Value.Split(new[] {'e', 'E'});
                    double radix = 0;
                    double.TryParse(items[0].Replace(".",","), out radix);
                    double exponent = 0;
                    double.TryParse(items[1],out exponent);
                    double value = Math.Pow(radix, exponent);
                    match.Result.SpanValue = value.ToString().AsMemory();
                    
                    match.Properties[GenericLexer<DoubleExponent>.DerivedToken] = DoubleExponent.DOUBLE_EXP;
                    return match;
                };
   	
                var fsmBuilder = lexer.FSMBuilder;

   	
                fsmBuilder.GoTo(GenericLexer<DoubleExponent>.in_double) // start a in_double node
                    .Transition(new char[] {'E','e'}) // add a transition on '.' with precondition
                    .Transition(new char[]{'+','-'})
                    .Mark("start_exponent_val")
                    .RangeTransitionTo('0','9',"start_exponent_val") // first year digit
                    .Mark("end_exponent")
                    .End(GenericToken.Extension) // mark as ending node 
                    .CallBack(callback); // set the ending callback
            }
        }
        
        private static void TestDoubleExponent()
        {
            var lex = LexerBuilder.BuildLexer<DoubleExponent>(AddExponentExtension);
            if (lex.IsOk)
            {
                var one = lex.Result.Tokenize("2.0E+2");
                ;
                var two = lex.Result.Tokenize("4.0e-2");
                ;
            }
        }

        public static void Test164() {
            var Parser = BuildParserExpression();
            var result = Parser.Result.Parse("1(1");
            if (result.IsError)
            {   
                foreach (var error in result.Errors)
                {
                    Console.WriteLine(error.ErrorMessage);
                }
            }

        }

        public static void Test192()
        {
            var  parser = Issue192.CreateBlockParser();
            var t = parser.Parse("A1   B2   ");
            if (t.IsOk)
            {
                Console.WriteLine("OK");
                ;
            }
            else
            {
                Console.WriteLine("KO");
                t.Errors.ForEach(x => Console.WriteLine(x.ErrorMessage));
                ;
            }
            ;
        }


        public static void TestIndentedLang()
        {
            string source = @"if truc == 1
  un = 1
  deux = 2
else
  if bidule ==89
         toto = 28
  trois = 3
  quatre = 4

";
Console.WriteLine("********************");
Console.WriteLine(source);
Console.WriteLine("********************");

            IndentedTest1(source);
            
            source = @"if truc == 1
  un = 1
  deux = 2
else  
  trois = 3
  quatre = 4
  if bidule ==89
     toto = 28
final = 9999
";
            Console.WriteLine("********************");
            Console.WriteLine(source);
            Console.WriteLine("********************");

            IndentedTest1(source);            
            //IndentedTest2(source);
        }

        private static void IndentedTest1(string source)
        {
            var lexRes = LexerBuilder.BuildLexer<IndentedLangLexer>();
            if (lexRes.IsOk)
            {
                var x = lexRes.Result.Tokenize(source);
                if (x.IsError)
                {
                    Console.WriteLine(x.Error.ErrorMessage);
                }
            }
            else
            {
                lexRes.Errors.ForEach(x => Console.WriteLine(x.Message));
            }

            ParserBuilder<IndentedLangLexer, Ast> builder = new ParserBuilder<IndentedLangLexer, Ast>();
            var instance = new IndentedParser();
            var parserRes = builder.BuildParser(instance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            if (parserRes.IsOk)
            {
                var res = parserRes.Result.Parse(source);
                if (res.IsOk)
                {
                    var r = res.Result;
                    Console.WriteLine(r.Dump(""));
                }
                else
                {
                    res.Errors.ForEach(x => Console.WriteLine(x.ErrorMessage));
                }
            }
            else
            {
                parserRes.Errors.ForEach(x => Console.WriteLine(x.Message));
            }
        }
        
        private static void IndentedTest2(string source)
        {
            var lexRes = LexerBuilder.BuildLexer<IndentedLangLexer2>();
            if (lexRes.IsOk)
            {
                var x = lexRes.Result.Tokenize(source);
                if (x.IsError)
                {
                    Console.WriteLine(x.Error.ErrorMessage);
                }
            }
            else
            {
                lexRes.Errors.ForEach(x => Console.WriteLine(x.Message));
            }

            ParserBuilder<IndentedLangLexer2, Ast> builder = new ParserBuilder<IndentedLangLexer2, Ast>();
            var instance = new IndentedParser2();
            var parserRes = builder.BuildParser(instance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            if (parserRes.IsOk)
            {
                var res = parserRes.Result.Parse(source);
                if (res.IsOk)
                {
                    var r = res.Result;
                    Console.WriteLine(r.Dump(""));
                }
                else
                {
                    res.Errors.ForEach(x => Console.WriteLine(x.ErrorMessage));
                }
            }
            else
            {
                parserRes.Errors.ForEach(x => Console.WriteLine(x.Message));
            }
        }

        private static void Main(string[] args)
        {
            //TestContextualParser();
            //TestTokenCallBacks();
            //test104();
            // testJSON();
            //TestGrammarParser();
            // TestGraphViz();
            // TestGraphViz();
            // TestChars();
            //TestAssociativityFactorExpressionParser();
            // TestFactorial();
            // TestIndentedFactorial();
            //TestThreadsafeGeneric();
            // TestManyString();
            //  TestDoubleExponent();
            //Test192();
            //TestRecursion();
            // TestFactorial();
            // TestThreadsafeGeneric();
            //Test177();
            //Test164();
            // TestIndentedLang();
            // TestI18N();
            // Console.ReadLine(); 
            // TestShortGeneric();
            //TestIssue239();
            TestShortOperations();
        }


        private static void Test177()
        {
            GenericLexerTests tests = new GenericLexerTests();
            tests.TestIssue177();
        }

        private static void TestShortGeneric()
        {
            var build = LexerBuilder.BuildLexer<GenericShortAttributes>();
            if (build.IsOk)
            {
                if (build.Result != null)
                {
                    var lexer = build.Result;
                    var lexResult = lexer.Tokenize(@"1 + 2 + a + b * 8.3 hello / 'b\'jour'");
                    if (lexResult.IsOk)
                    {
                        lexResult.Tokens.ForEach(x => Console.WriteLine(x));
                    }
                    else
                    {
                        Console.WriteLine(lexResult.Error.ErrorMessage);
                    }
                }
            }
            else
            {
                foreach (var error in build.Errors)
                {
                    Console.WriteLine(error.Message);
                }
            }
            ;
        }

        private static void TestShortOperations()
        {
            var startingRule = $"{typeof(ShortOperationAttributesParser).Name}_expressions";
            var parserInstance = new ShortOperationAttributesParser();
            var builder = new ParserBuilder<ExpressionToken, double>();
            var buildResult = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, startingRule);
            Assert.True(buildResult.IsOk);
            var parser = buildResult.Result;
            Assert.NotNull(parser);
            var result = parser.Parse("-1 +2 * (5 + 6) - 4 ");
            Assert.Equal(-1+2*(5+6)-4, result.Result);
        }

        private static void TestIssue239()
        {
            Issue239Tests.TestOk();
            ;
        }
    }

    public enum TestGrammarToken
    {
        [Lexeme(GenericToken.SugarToken,",")]
        COMMA = 1
    }
    
    

    public class ErroneousGrammar
    {
        [Production("clauses : clause (COMMA [D] clause)*")]

            public object test()
            {
                return null;
            }    
    }

    public class RecursiveGrammar
    {
        [Production("first : second COMMA[d]")]
        public object FirstRecurse(object o)
        {
            return o;
        } 
        
        [Production("second : third COMMA[d]")]
        public object SecondRecurse(object o)
        {
            return o;
        }
        
        [Production("third : first COMMA[d]")]
        public object ThirdRecurse(object o)
        {
            return o;
        }
    }
    
    public class RecursiveGrammar2
    {
        [Production("first : second* third COMMA[d]")]
        public object FirstRecurse(List<object> seconds, object third)
        {
            return third;
        } 
        
        [Production("first : second? third COMMA[d]")]
        public object FirstRecurse2(ValueOption<object> optSecond, object third)
        {
            return null;
        }
        
        [Production("second :  COMMA[d]")]
        public object SecondRecurse()
        {
            return null;
        }
        
        [Production("third : first COMMA[d]")]
        public object ThirdRecurse(object o)
        {
            return o;
        }
    }
    
}