/*
    Author: Christian Hahm
    Created: May 24, 2022
    Purpose: Holds data structure implementations that are specific / custom to NARS
*/
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using static NARSGenome;


public class Buffer<T> : ItemContainer<T>
{
    PriorityQueue<Item<T>, float> priority_queue;

    public Buffer(int capacity) : base(capacity)
    {
        this.priority_queue = new PriorityQueue<Item<T>, float>(new DecendingComparer<float>());
    }

    public Item<T>? take() {
        /*
            Take the max priority item
            :return:
        */
        if(this.GetCount() == 0) return null;
        Item<T> item = this.priority_queue.Dequeue();
        this._take_from_lookup_dict(item.key);
        return item;
    }

    public Item<T>? peek(string? key) {
        /*
            Peek item with highest priority
            O(1)

            Returns None if depq is empty
        */
        if(this.GetCount() == 0) return null;
        if (key == null) {
            return this.priority_queue.First;
        }
        else {
            return base.peek_using_key(key);
        }

    }


    public override Item<T> PUT_NEW(T obj)
    {
        Item<T> item = base.PUT_NEW(obj);
        this.priority_queue.Enqueue(item, item.budget.get_priority());
        return item;
    }

    class DecendingComparer<TKey> : IComparer<float>
    {
        public int Compare(float x, float y)
        {
            return y.CompareTo(x);
        }
    }
}


public class TemporalModule
{
    /*
        Performs temporal composition
                and
            anticipation (negative evidence for predictive implications)
    */
    private readonly NARS nars;
    private readonly int capacity;
    private readonly List<Judgment> temporal_chain;

    public TemporalModule(NARS nars, int capacity)
    {
        this.nars = nars;
        this.capacity = capacity;
        this.temporal_chain = new List<Judgment>(capacity);
    }

    /// <summary>
    /// Inserts a new judgment, keeps list sorted by occurrence_time,
    /// and pops the oldest if capacity is exceeded.
    /// </summary>
    public Judgment PUT_NEW(Judgment obj)
    {
        // Insert in sorted order by occurrence_time
        int idx = temporal_chain.BinarySearch(obj, JudgmentTimeComparer.Instance);
        if (idx < 0) idx = ~idx; // BinarySearch returns bitwise complement of insert index
        temporal_chain.Insert(idx, obj);

        // Check capacity
        Judgment popped = null;
        if (temporal_chain.Count > capacity)
        {
            // Oldest = first element (smallest occurrence_time)
            popped = temporal_chain[0];
            temporal_chain.RemoveAt(0);
        }

        this.process_temporal_chaining();

        return popped;
    }

    public int GetCount() => temporal_chain.Count;

    // Optional: expose read-only access
    public IReadOnlyList<Judgment> Items => temporal_chain.AsReadOnly();

    /// <summary>
    /// Comparer for sorting judgments by occurrence_time
    /// </summary>
    private class JudgmentTimeComparer : IComparer<Judgment>
    {
        public static readonly JudgmentTimeComparer Instance = new JudgmentTimeComparer();
        public int Compare(Judgment x, Judgment y)
        {
            return x.stamp.occurrence_time.CompareTo(y.stamp.occurrence_time);
        }
    }


    void process_temporal_chaining() {
        if (this.GetCount() >= 3)
        {
            this.temporal_chaining();
            //this.temporal_chaining_2_imp();
        }

    }

    public Judgment GetMostRecentEventTask()
    {
        if (temporal_chain == null || temporal_chain.Count == 0)
            return null;

        // Assuming Judgment.Object is actually an EventTask
        return temporal_chain[^1];
    }


    public void temporal_chaining()
    {
        /*
            Perform temporal chaining

            Produce all possible forward implication statements using temporal induction && intersection (A && B)

            for the latest statement in the chain
        */

        var temporalChain = this.temporal_chain;
        int numOfEvents = temporalChain.Count;

        if (numOfEvents == 0) return;



        // Loop over all earlier events A
        for (int i = 0; i < numOfEvents - 2; i++)
        {
            var eventA = temporalChain[i];
            if (eventA == null) continue;



            // Validate
            if (!(eventA.statement is StatementTerm))
            {
                continue;
            }
            for (int j = i + 1; j < numOfEvents - 1; j++)
            {
                var eventB = temporalChain[j];
                if (eventA == null) continue;



                // Validate
                if (!(eventB.statement is StatementTerm))
                {
                    continue;
                }

                for (int k = j + 1; k < numOfEvents; k++)
                {
                    var eventC = temporalChain[k];
                    if (eventC == null) continue;
                    // Validate
                    if (!(eventC.statement is StatementTerm))
                    {
                        continue;
                    }

                    if (eventA.statement == eventB.statement || eventA.statement == eventC.statement || eventB.statement == eventC.statement)
                    {
                        continue;
                    }
                    if (!eventB.statement.is_op()) continue;
                    if (eventA.statement.is_op() || eventC.statement.is_op()) continue;
         
                    // Do inference
                    var conjunction = this.nars.inferenceEngine.temporalRules.TemporalIntersection(eventA, eventB);
                 
                    conjunction.stamp.occurrence_time = eventA.stamp.occurrence_time;
                    var implication = (Judgment)this.nars.inferenceEngine.temporalRules.TemporalInduction(conjunction, eventC);
                    implication.evidential_value.frequency = 1.0f;
                    implication.evidential_value.confidence = this.nars.helperFunctions.get_unit_evidence();
                  
                    this.nars.global_buffer.PUT_NEW(implication);

                    if (NARSGenome.USE_GENERALIZATION)
                    {
                        Judgment generalization = Generalize(implication);
                        if (generalization != null)
                        {
                            this.nars.global_buffer.PUT_NEW(generalization);
                        }

                    }

                }

            }
        }
    }

    public Judgment Generalize(Judgment concrete_implication) { 
    


        string old_statement_string = concrete_implication.statement.ToString();

        // (S &/ ^M =/> P)
        StatementTerm implication = (StatementTerm)concrete_implication.statement;

        CompoundTerm subject = (CompoundTerm)implication.get_subject_term();
        StatementTerm predicate = (StatementTerm)implication.get_predicate_term();

        StatementTerm new_statement;

        StatementTerm S = (StatementTerm)subject.subterms[0];
        StatementTerm M = (StatementTerm)subject.subterms[1];

        Term S_predicate = S.get_predicate_term();
        Term M_argument = ((CompoundTerm)M.get_subject_term()).subterms[1];

        StatementTerm new_S = null;
        StatementTerm new_M = null;

        if (S_predicate is AtomicTerm && M_argument is AtomicTerm)
        {
            if (S_predicate != M_argument) return null; // invalid for generalization
            // turn from concrete term into variable
            new_S = new StatementTerm(S.get_subject_term(), new VariableTerm("x", VariableTerm.VariableType.Dependent), Copula.Inheritance);
            new_M = new StatementTerm(Term.from_string("(*,{SELF},#x)"), M.get_predicate_term(), Copula.Inheritance);
        }
        else
        {
            Debug.LogError("Error");
            return null;
        }

        if(predicate == energy_increasing)
        {
            int h = 1;
            UnityEngine.Debug.LogError("generalization formed");
        }

        new_statement = CreateContingencyStatement(new_S, new_M, predicate);

        Judgment generalization = new(this.nars, new_statement, new(1.0f,this.nars.config.GENERALIZATION_CONFIDENCE));

        return generalization;
    }


    public struct Anticipation
    {
        public Term term_expected;
        public int time_remaining;
    }

    public List<Anticipation> anticipations = new();
    Dictionary<Term, int> anticipations_dict = new();
    public void Anticipate(Term term_to_anticipate)
    {
        Anticipation anticipation = new Anticipation();
        anticipation.term_expected = term_to_anticipate;
        anticipation.time_remaining = this.nars.config.ANTICIPATION_WINDOW;
        anticipations.Add(anticipation);
        if (anticipations_dict.ContainsKey(term_to_anticipate))
        {
            anticipations_dict[term_to_anticipate]++;
        }
        else
        {
            anticipations_dict.Add(term_to_anticipate, 1);
        }
    }

    public void UpdateAnticipations()
    {
        for (int i = anticipations.Count - 1; i >= 0; i--)
        {
            Anticipation a = anticipations[i];

            a.time_remaining--;

            if (a.time_remaining <= 0)
            {
                anticipations.RemoveAt(i);
                anticipations_dict[a.term_expected]--;
                if (anticipations_dict[a.term_expected] <= 0)
                {
                    anticipations_dict.Remove(a.term_expected);
                }

                // disappoint; the anticipation failed
                var disappoint = new Judgment(this.nars, a.term_expected,new EvidentialValue(0.0f,this.nars.helperFunctions.get_unit_evidence()));
                this.nars.global_buffer.PUT_NEW(disappoint);
            }
            else
            {
                anticipations[i] = a; // write back updated struct
            }
        }
    }

    internal bool DoesAnticipate(Term term)
    {
        return anticipations_dict.ContainsKey(term);
    }

    public void RemoveAnticipations(Term term)
    {
        for (int i = anticipations.Count - 1; i >= 0; i--)
        {
            Anticipation a = anticipations[i];

            if (a.term_expected == term) 
            {
                anticipations.RemoveAt(i);
            }
        }
        anticipations_dict.Remove(term);
    }
}

   