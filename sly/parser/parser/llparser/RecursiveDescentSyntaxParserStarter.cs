﻿using System.Collections.Generic;
using System.Linq;
using sly.lexer;
using sly.parser.generator;
using sly.parser.syntax.grammar;
using sly.parser.syntax.tree;

namespace sly.parser.llparser
{
    public partial class RecursiveDescentSyntaxParser<IN, OUT> : ISyntaxParser<IN, OUT> where IN : struct
    {
      

        public ParserConfiguration<IN, OUT> Configuration { get; set; }
        public string StartingNonTerminal { get; set; }
        
        public string I18n { get; set; }

          public ParserConfiguration<IN, OUT> ComputeSubRules(ParserConfiguration<IN, OUT> configuration)
        {
            var newNonTerms = new List<NonTerminal<IN>>();
            foreach (var nonTerm in configuration.NonTerminals)
            foreach (var rule in nonTerm.Value.Rules)
            {
                var newclauses = new List<IClause<IN>>();
                if (rule.ContainsSubRule)
                {
                    foreach (var clause in rule.Clauses)
                        if (clause is GroupClause<IN> group)
                        {
                            var newNonTerm = CreateSubRule(group);
                            newNonTerms.Add(newNonTerm);
                            var newClause = new NonTerminalClause<IN>(newNonTerm.Name);
                            newClause.IsGroup = true;
                            newclauses.Add(newClause);
                        }
                        else if (clause is ManyClause<IN> many)
                        {
                            if (many.Clause is GroupClause<IN> manyGroup)
                            {
                                var newNonTerm = CreateSubRule(manyGroup);
                                newNonTerms.Add(newNonTerm);
                                var newInnerNonTermClause = new NonTerminalClause<IN>(newNonTerm.Name);
                                newInnerNonTermClause.IsGroup = true;
                                many.Clause = newInnerNonTermClause;
                                newclauses.Add(many);
                            }
                        }
                        else if (clause is OptionClause<IN> option)
                        {
                            if (option.Clause is GroupClause<IN> optionGroup)
                            {
                                var newNonTerm = CreateSubRule(optionGroup);
                                newNonTerms.Add(newNonTerm);
                                var newInnerNonTermClause = new NonTerminalClause<IN>(newNonTerm.Name);
                                newInnerNonTermClause.IsGroup = true;
                                option.Clause = newInnerNonTermClause;
                                newclauses.Add(option);
                            }
                        }
                        else
                        {
                            newclauses.Add(clause);
                        }

                    rule.Clauses.Clear();
                    rule.Clauses.AddRange(newclauses);
                }
            }

            newNonTerms.ForEach(nonTerminal => configuration.AddNonTerminalIfNotExists(nonTerminal));
            return configuration;
        }

        public NonTerminal<IN> CreateSubRule(GroupClause<IN> group)
        {
            var subRuleNonTerminalName = "GROUP-" + group.Clauses.Select(c => c.ToString())
                                             .Aggregate((c1, c2) => $"{c1.ToString()}-{c2.ToString()}");
            var nonTerminal = new NonTerminal<IN>(subRuleNonTerminalName);
            var subRule = new Rule<IN>();
            subRule.Clauses = group.Clauses;
            subRule.IsSubRule = true;
            nonTerminal.Rules.Add(subRule);
            nonTerminal.IsSubRule = true;

            return nonTerminal;
        }

        #region STARTING_TOKENS

        protected virtual void InitializeStartingTokens(ParserConfiguration<IN, OUT> configuration, string root)
        {
            var nts = configuration.NonTerminals;


            InitStartingTokensForNonTerminal(nts, root);
            foreach (var nt in nts.Values)
            {
                foreach (var rule in nt.Rules)
                {
                    if (rule.PossibleLeadingTokens == null || rule.PossibleLeadingTokens.Count == 0)
                        InitStartingTokensForRule(nts, rule);
                }
            }
        }

        protected virtual void InitStartingTokensForNonTerminal(Dictionary<string, NonTerminal<IN>> nonTerminals,
            string name)
        {
            if (nonTerminals.ContainsKey(name))
            {
                var nt = nonTerminals[name];
                nt.Rules.ForEach(r => InitStartingTokensForRule(nonTerminals, r));
            }
        }

        protected void InitStartingTokensForRule(Dictionary<string, NonTerminal<IN>> nonTerminals,
            Rule<IN> rule)
        {
            if (rule.PossibleLeadingTokens == null || rule.PossibleLeadingTokens.Count == 0)
            {
                rule.PossibleLeadingTokens = new List<IN>();
                if (rule.Clauses.Count > 0)
                {
                    var first = rule.Clauses[0];
                    if (first is TerminalClause<IN> term)
                    {
                        rule.PossibleLeadingTokens.Add(term.ExpectedToken);
                        rule.PossibleLeadingTokens = rule.PossibleLeadingTokens.Distinct().ToList();
                    }
                    else if (first  is NonTerminalClause<IN> nonterm)
                    {
                        InitStartingTokensForNonTerminal(nonTerminals, nonterm.NonTerminalName);
                        if (nonTerminals.ContainsKey(nonterm.NonTerminalName))
                        {
                            var firstNonTerminal = nonTerminals[nonterm.NonTerminalName];
                            firstNonTerminal.Rules.ForEach(r =>
                            {
                                rule.PossibleLeadingTokens.AddRange(r.PossibleLeadingTokens);
                            });
                            rule.PossibleLeadingTokens = rule.PossibleLeadingTokens.Distinct().ToList();
                        }
                    }
                    else
                    {
                        InitStartingTokensForRuleExtensions(first,rule,nonTerminals);
                    }
                }
            }
        }

        protected virtual void InitStartingTokensForRuleExtensions(IClause<IN> first, Rule<IN> rule,
            Dictionary<string, NonTerminal<IN>> nonTerminals)
        {
        }

        #endregion


    }
}