/*
    Author: Christian Hahm
    Created: May 13, 2022
    Purpose: Enforces Narsese grammar that is used throughout the project
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public abstract class Sentence
{
    /*
        sentence::= <statement><punctuation> <tense> %<value>%
    */

    readonly public Term statement;
    readonly public Punctuation punctuation;
    readonly public Stamp stamp;
    public EvidentialValue evidential_value;
    readonly public float eternal_expectation;

    //metadata
    public bool needs_to_be_answered_in_output;
    public bool is_from_input;

    public Sentence(NARS nars, Term statement, EvidentialValue value, Punctuation punctuation, int occurrence_time = -1)
    {
        /*

        :param statement:
        :param value: Pass as a tuple for array sentences (overall_truth, list_of_element_truth_values)
        :param punctuation:
        :param occurrence_time:
        */
        //Asserts.assert_punctuation(punctuation);
        //Asserts.assert_valid_statement(statement);

        this.statement = statement;
        this.punctuation = punctuation;
        this.stamp = new Stamp(nars, this, occurrence_time);
        this.evidential_value = value;  // truth-value (for Judgment) || desire-value (for Goal) || null (for Question)

        if (this.punctuation != Punctuation.Question)
        {
            this.eternal_expectation = TruthValueFunctions.Expectation(this.evidential_value.frequency, this.evidential_value.confidence);
        }
    }

    public override string ToString()
    {
        
        return this.statement.ToString() 
            + PunctuationMethods.get_string_from_punctuation(this.punctuation) 
            + " " + this.evidential_value.ToString();
    }


    public bool is_event()
    {
        return this.stamp.occurrence_time != -1;
    }

    /// <summary>
    /// Gets the tense of this sentence compared to the given cycle
    /// </summary>
    /// <param name="cycle"></param>
    /// <returns></returns>
    public Tense get_tense(int cycle)
    {
        return this.stamp.get_tense(cycle);
    }

    public bool is_eternal()
    {
        return this.stamp.is_eternal;
    }


    public static Sentence new_sentence_from_string(NARS nars, string sentence_string, int current_cycle_number)
    {
        /*
            :param sentence_string - String of NAL syntax <term copula term>punctuation %frequency;confidence%

            :returns Sentence parsed from sentence_string
        */
        // Find statement start && statement end
        int start_idx = sentence_string.IndexOf(SyntaxUtils.stringValueOf(StatementSyntax.Start));
        //Asserts.assert(start_idx != -1, "Statement start character " + SyntaxUtils.stringValueOf(StatementSyntax.Start) + " not found.");

        int end_idx = sentence_string.LastIndexOf(SyntaxUtils.stringValueOf(StatementSyntax.End));
        //Asserts.assert(end_idx != -1, "Statement end character " + SyntaxUtils.stringValueOf(StatementSyntax.End) + " not found.");

        // Find sentence punctuation
        int punctuation_idx = end_idx + 1;
        //Asserts.assert(punctuation_idx < sentence_string.Length, "No punctuation found.");
        string punctuation_str = Char.ToString(sentence_string[punctuation_idx]);
        Punctuation punctuation = PunctuationMethods.get_punctuation_from_string(punctuation_str);
        //Asserts.assert(punctuation != null, punctuation_str + " == not punctuation.");

        // Find Truth Value, if it exists
        int start_truth_val_idx = sentence_string.IndexOf(SyntaxUtils.stringValueOf(StatementSyntax.TruthValMarker), punctuation_idx);
        int middle_truth_val_idx = sentence_string.IndexOf(SyntaxUtils.stringValueOf(StatementSyntax.ValueSeparator), punctuation_idx);
        int end_truth_val_idx = sentence_string.LastIndexOf(SyntaxUtils.stringValueOf(StatementSyntax.TruthValMarker), punctuation_idx);

        bool truth_value_found = !(start_truth_val_idx == -1 || end_truth_val_idx == -1 || start_truth_val_idx == end_truth_val_idx);
        float? freq = null;
        float? conf = null;
        if (truth_value_found)
        {
            // Parse truth value from string
            freq = float.Parse(sentence_string[(start_truth_val_idx + 1)..middle_truth_val_idx]);
            conf = float.Parse(sentence_string[(middle_truth_val_idx + 1)..end_truth_val_idx]);
        }

        // create the statement
        string statement_string = sentence_string[start_idx..(end_idx + 1)];
        Term statement = TermHelperFunctions.simplify(Term.from_string(statement_string));


        // Find Tense, if it exists
        // otherwise mark it as eternal
        Tense tense = Tense.Eternal;
        string[] tenses = Enum.GetNames(typeof(Tense));
        foreach (string t in tenses)
        {
            if (t != SyntaxUtils.stringValueOf(Tense.Eternal))
            {
                int tense_idx = sentence_string.IndexOf(t);
                if (tense_idx != -1)
                {   // found a tense
                    tense = (Tense)SyntaxUtils.enumValueOf<Tense>(sentence_string[tense_idx..(tense_idx + t.Length)]);
                    break;
                }
            }
        }


        Sentence sentence = null;
        // make sentence
        if (punctuation == Punctuation.Judgment)
        {
            EvidentialValue value;
            if (freq != null)
            {
                value = new EvidentialValue((float)freq, (float)conf);
            }
            else
            {
                value = new EvidentialValue(1.0f, nars.helperFunctions.get_unit_evidence());
            }
            sentence = new Judgment(nars,statement, value);
        }

        else if (punctuation == Punctuation.Question)
        {
            sentence = new Question(nars, statement);
        }
        else if (punctuation == Punctuation.Goal)
        {
            EvidentialValue value;
            if (freq != null)
            {
                value = new EvidentialValue((float)freq, (float)conf);
            }
            else
            {
                value = new EvidentialValue(1.0f, nars.helperFunctions.get_unit_evidence());
            }
            sentence = new Goal(nars,statement, value);
        }
        else
        {
            //Asserts.assert(false, "Error: No Punctuation!");
        }



        if (tense == Tense.Present)
        {
            // Mark present tense event as happening right now!
            sentence.stamp.occurrence_time = current_cycle_number;
        }

        return sentence;
    }

    public Term get_statement_term()
    {
        return this.statement;
    }

}



public class Judgment : Sentence
{
    /*
        judgment ::= <statement>. %<truth-value>%
    */

    public Judgment(NARS nars, Term statement, EvidentialValue value, int occurrence_time = -1) : base(nars, statement, value, Punctuation.Judgment, occurrence_time) { }

}


public class Question : Sentence
{
    /*
        question ::= <statement>? %<truth-value>%
    */

    public Question(NARS nars, Term statement) : base(nars, statement, default, Punctuation.Question)
    {

    }
}


public class Goal : Sentence
{
    /*
        goal ::= <statement>! %<desire-value>%
    */
    bool executed;

    public Goal(NARS nars, Term statement, EvidentialValue value, int occurrence_time = -1) : base(nars, statement, value, Punctuation.Goal, occurrence_time)
    {
        this.executed = false;
    }
    public float get_desirability(NARS nars)
    {
        return nars.inferenceEngine.get_desirability(this);
    }

    // since confidence = 0 produces desiraility of 0.5,
    // the motor activation has to be scaled such that <= 0.5 is zero,
    // and [0.5,1.0] is normalized to [0,1]
    public float get_motor_activation(NARS nars)
    {
        float desirability = nars.inferenceEngine.get_desirability(this);

        return (desirability - 0.5f) * 2f;
    }
}



public class Stamp
{
    /*
        Defines the metadata of a sentence, including
        when it was created, its occurrence time (when is its truth value valid),
        evidential base, etc.
    */

    public bool is_eternal; 
    public int id;
    public int occurrence_time = -1;
    public Sentence sentence;
    public EvidentialBase evidential_base;
    public string derived_by;
    public bool from_one_premise_inference;

    public Stamp(NARS nars ,Sentence this_sentence, int occurrence_time = -1)
    {
        this.id = nars.NEXT_STAMP_ID++;
        this.occurrence_time = occurrence_time;
        this.sentence = this_sentence;
        this.evidential_base = new EvidentialBase(nars, this_sentence);
        this.derived_by = null; // none if input task
        this.from_one_premise_inference = false; // == this sentence derived from one-premise inference?
        this.is_eternal = (occurrence_time == -1);
    }

    public Tense get_tense(int cycle)
    {
        if (this.occurrence_time == null) return Tense.Eternal;

        if (this.occurrence_time < cycle)
        {
            return Tense.Past;
        }
        else if (this.occurrence_time == cycle)
        {
            return Tense.Present;
        }
        else
        {
            return Tense.Future;
        }
    }
}


public class EvidentialBase : IEnumerable<Sentence>
{
    /*
        Stores history of how the sentence was derived
    */
    Sentence sentence;
    private HashSet<Sentence> evidential_base;
    private Queue<Sentence> evidential_base_list;
    NARS nars;
    public EvidentialBase(NARS nars ,Sentence this_sentence)
    {
        /*
        :param id: Sentence ID
        */
        this.nars = nars;
        this.sentence = this_sentence;
        this.evidential_base = new (this.nars.config.MAX_EVIDENTIAL_BASE_LENGTH);  // array of sentences
        this.evidential_base_list = new (this.nars.config.MAX_EVIDENTIAL_BASE_LENGTH);  // array of sentences
        this.evidential_base.Add(this_sentence);
        this.evidential_base_list.Enqueue(this_sentence);
    }
    public IEnumerator<Sentence> GetEnumerator()
    {
        foreach (var item in this.evidential_base)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)evidential_base).GetEnumerator();
    }

    public bool Contains(Sentence j)
    {
        return this.evidential_base.Contains(j);
    }

    public void merge_sentence_evidential_base_into_this(Sentence sentence)
    {
        /*
            Merge a Sentence's evidential base into this.
            This function assumes the base to merge does not have evidential overlap with this base
            #todo figure out good way to store evidential bases such that older evidence == purged on overflow
        */
        foreach (Sentence e_sentence in sentence.stamp.evidential_base)
        {
            if (!this.evidential_base.Add(e_sentence))
                continue; // skip duplicates

            this.evidential_base_list.Enqueue(e_sentence);

            if (this.evidential_base.Count > this.nars.config.MAX_EVIDENTIAL_BASE_LENGTH)
            {
                var oldest = this.evidential_base_list.Dequeue();
                this.evidential_base.Remove(oldest);
            }
        }

    }


    public bool has_evidential_overlap(EvidentialBase other_base)
    {
        /*
            Check does other base has overlapping evidence with this?
            O(M + N)
            https://stackoverflow.com/questions/3170055/test-if-lists-share-any-items-in-python
        */
        //if (this.sentence.is_event()) return false;
        // return this.evidential_base.Intersect(other_base.evidential_base).Any();
        return this.evidential_base.Overlaps(other_base.evidential_base);
    }


    public static bool may_interact(Sentence j1, Sentence j2)
    {
        /*
            2 Sentences may interact if:
                // Neither is "null"
                // They are not the same Sentence
                // They have not previously interacted
                // One is not in the other's evidential base
                // They do not have overlapping evidential base
        :param j1:
        :param j2:
        :return: Are the sentence allowed to interact for inference
        */
        if (j1.is_event() || j2.is_event()) return true;
        if (j1 == null || j2 == null) return false;
        if (j1.stamp.id == j2.stamp.id) return false;
        if (j2.stamp.evidential_base.Contains(j1)) return false;
        if (j1.stamp.evidential_base.Contains(j2)) return false;
        if (j1.stamp.evidential_base.has_evidential_overlap(j2.stamp.evidential_base)) return false;
        return true;
    }


}



