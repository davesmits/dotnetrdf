﻿/*

Copyright Robert Vesse 2009-10
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing.Contexts;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Writing
{
    /// <summary>
    /// Class for generating Notation 3 Concrete RDF Syntax which provides varying levels of Syntax Compression
    /// </summary>
    /// <threadsafety instance="true">Designed to be Thread Safe - should be able to call the Save() method from multiple threads on different Graphs without issue</threadsafety>
    public class Notation3Writer : IRdfWriter, IPrettyPrintingWriter, IHighSpeedWriter, ICompressingWriter
    {
        private bool _prettyprint = true;
        private bool _allowHiSpeed = true;
        private int _compressionLevel = WriterCompressionLevel.Default;

        /// <summary>
        /// Creates a new Notation 3 Writer which uses the Default Compression Level
        /// </summary>
        public Notation3Writer()
        {

        }

        /// <summary>
        /// Creates a new Notation 3 Writer which uses the given Compression Level
        /// </summary>
        /// <param name="compressionLevel">Desired Compression Level</param>
        /// <remarks>See Remarks for this classes <see cref="Notation3Writer.CompressionLevel">CompressionLevel</see> property to see what effect different compression levels have</remarks>
        public Notation3Writer(int compressionLevel)
        {
            this._compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Gets/Sets whether Pretty Printing is used
        /// </summary>
        public bool PrettyPrintMode
        {
            get
            {
                return this._prettyprint;
            }
            set
            {
                this._prettyprint = value;
            }
        }

        /// <summary>
        /// Gets/Sets whether High Speed Write Mode should be allowed
        /// </summary>
        public bool HighSpeedModePermitted
        {
            get
            {
                return this._allowHiSpeed;
            }
            set
            {
                this._allowHiSpeed = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Compression Level to be used
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the Compression Level is set to <see cref="WriterCompressionLevels.None">None</see> then High Speed mode will always be used regardless of the input Graph and the <see cref="Notation3Writer.HighSpeedModePermitted">HighSpeedMorePermitted</see> property.
        /// </para>
        /// <para>
        /// If the Compression Level is set to <see cref="WriterCompressionLevels.Minimal">Minimal</see> or above then full Predicate Object lists will be used for Triples.
        /// </para>
        /// <para>
        /// If the Compression Level is set to <see cref="WriterCompressionLevels.More">More</see> or above then Blank Node Collections and Collection syntax will be used if the Graph contains Triples that can be compressed in that way.</para>
        /// </remarks>
        public int CompressionLevel
        {
            get
            {
                return this._compressionLevel;
            }
            set
            {
                this._compressionLevel = value;
            }
        }

        /// <summary>
        /// Saves a Graph to a file using Notation 3 Syntax
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="filename">File to save to</param>
        public void Save(IGraph g, string filename)
        {
            this.Save(g, new StreamWriter(filename, false, Encoding.UTF8));
        }

        /// <summary>
        /// Saves a Graph to the given Stream using Notation 3 Syntax
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="output">Stream to save to</param>
        public void Save(IGraph g, TextWriter output)
        {
            try
            {
                CompressingTurtleWriterContext context = new CompressingTurtleWriterContext(g, output, this._compressionLevel, this._prettyprint, this._allowHiSpeed);
                context.NodeFormatter = new Notation3Formatter();
                this.GenerateOutput(context);
            }
            finally
            {
                try
                {
                    output.Close();
                }
                catch
                {
                    //No Catch actions - just trying to clean up
                }
            }
        }

        /// <summary>
        /// Generates the Notation 3 Syntax for the Graph
        /// </summary>
        private void GenerateOutput(CompressingTurtleWriterContext context)
        {
            //Create the Header
            //Base Directive
            if (context.Graph.BaseUri != null)
            {
                context.Output.WriteLine("@base <" + context.UriFormatter.FormatUri(context.Graph.BaseUri) + ">.");
                context.Output.WriteLine();
            }
            //Prefix Directives
            foreach (String prefix in context.Graph.NamespaceMap.Prefixes)
            {
                if (!prefix.Equals(String.Empty))
                {
                    context.Output.WriteLine("@prefix " + prefix + ": <" + context.UriFormatter.FormatUri(context.Graph.NamespaceMap.GetNamespaceUri(prefix)) + ">.");
                }
                else
                {
                    context.Output.WriteLine("@prefix : <" + context.UriFormatter.FormatUri(context.Graph.NamespaceMap.GetNamespaceUri(String.Empty)) + ">.");
                }
            }
            context.Output.WriteLine();

            //Decide on the Write Mode to use
            bool hiSpeed = false;
            double subjNodes = context.Graph.Triples.SubjectNodes.Count();
            double triples = context.Graph.Triples.Count;
            if ((subjNodes / triples) > 0.75) hiSpeed = true;

            if (context.CompressionLevel == WriterCompressionLevel.None || (hiSpeed && context.HighSpeedModePermitted))
            {
                this.RaiseWarning("High Speed Write Mode in use - minimal syntax compression will be used");
                context.CompressionLevel = WriterCompressionLevel.Minimal;
                context.NodeFormatter = new UncompressedNotation3Formatter();

                foreach (Triple t in context.Graph.Triples)
                {
                    context.Output.WriteLine(this.GenerateTripleOutput(context, t));
                }
            }
            else
            {
                if (context.CompressionLevel >= WriterCompressionLevel.More)
                {
                    WriterHelper.FindCollections(context);
                }

                //Get the Triples as a Sorted List
                List<Triple> ts = context.Graph.Triples.Where(t => !context.TriplesDone.Contains(t)).ToList();
                ts.Sort();

                //Variables we need to track our writing
                INode lastSubj, lastPred;
                lastSubj = lastPred = null;
                int subjIndent = 0, predIndent = 0;
                String temp;

                for (int i = 0; i < ts.Count; i++)
                {
                    Triple t = ts[i];
                    if (lastSubj == null || !t.Subject.Equals(lastSubj))
                    {
                        //Terminate previous Triples
                        if (lastSubj != null) context.Output.WriteLine(".");

                        //Start a new set of Triples
                        temp = this.GenerateNodeOutput(context, t.Subject, TripleSegment.Subject);
                        context.Output.Write(temp);
                        context.Output.Write(" ");
                        subjIndent = temp.Length + 1;
                        lastSubj = t.Subject;

                        //Write the first Predicate
                        temp = this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate);
                        context.Output.Write(temp);
                        context.Output.Write(" ");
                        predIndent = temp.Length + 1;
                        lastPred = t.Predicate;
                    }
                    else if (lastPred == null || !t.Predicate.Equals(lastPred))
                    {
                        //Terminate previous Predicate Object list
                        context.Output.WriteLine(";");

                        if (context.PrettyPrint) context.Output.Write(new String(' ', subjIndent));

                        //Write the next Predicate
                        temp = this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate);
                        context.Output.Write(temp);
                        context.Output.Write(" ");
                        predIndent = temp.Length + 1;
                        lastPred = t.Predicate;
                    }
                    else
                    {
                        //Continue Object List
                        context.Output.WriteLine(",");

                        if (context.PrettyPrint) context.Output.Write(new String(' ', subjIndent + predIndent));
                    }

                    //Write the Object
                    context.Output.Write(this.GenerateNodeOutput(context, t.Object, TripleSegment.Object));
                }

                //Terminate Triples
                if (ts.Count > 0) context.Output.WriteLine(".");

                return;
            }

        }

        /// <summary>
        /// Generates Output for Triples as a single "s p o." Triple
        /// </summary>
        /// <param name="context">Writer Context</param>
        /// <param name="t">Triple to output</param>
        /// <returns></returns>
        /// <remarks>Used only in High Speed Write Mode</remarks>
        private String GenerateTripleOutput(CompressingTurtleWriterContext context, Triple t)
        {
            StringBuilder temp = new StringBuilder();
            temp.Append(this.GenerateNodeOutput(context, t.Subject, TripleSegment.Subject));
            temp.Append(' ');
            temp.Append(this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate));
            temp.Append(' ');
            temp.Append(this.GenerateNodeOutput(context, t.Object, TripleSegment.Object));
            temp.Append('.');

            return temp.ToString();
        }

        /// <summary>
        /// Generates Output for Nodes in Notation 3 syntax
        /// </summary>
        /// <param name="context">Writer Context</param>
        /// <param name="n">Node to generate output for</param>
        /// <param name="segment">Segment of the Triple being output</param>
        /// <returns></returns>
        private String GenerateNodeOutput(CompressingTurtleWriterContext context, INode n, TripleSegment segment)
        {
            StringBuilder output = new StringBuilder();

            switch (n.NodeType)
            {
                case NodeType.Blank:
                    if (context.Collections.ContainsKey(n))
                    {
                        output.Append(this.GenerateCollectionOutput(context, context.Collections[n]));
                    }
                    else
                    {
                        return context.NodeFormatter.Format(n);
                    }
                    break;

                case NodeType.GraphLiteral:
                    output.Append("{");
                    GraphLiteralNode glit = (GraphLiteralNode)n;

                    CompressingTurtleWriterContext subcontext = new CompressingTurtleWriterContext(glit.SubGraph, null);

                    //Write Triples 1 at a Time on a single line
                    foreach (Triple t in subcontext.Graph.Triples) 
                    {
                        output.Append(this.GenerateNodeOutput(subcontext, t.Subject, TripleSegment.Subject));
                        output.Append(" ");
                        output.Append(this.GenerateNodeOutput(subcontext, t.Predicate, TripleSegment.Predicate));
                        output.Append(" ");
                        output.Append(this.GenerateNodeOutput(subcontext, t.Object, TripleSegment.Object));
                        output.Append(". ");
                    }

                    output.Append("}");
                    break;

                case NodeType.Literal:
                    if (segment == TripleSegment.Predicate) throw new RdfOutputException(WriterErrorMessages.LiteralPredicatesUnserializable("Notation 3"));
                    return context.NodeFormatter.Format(n);

                case NodeType.Uri:
                    return context.NodeFormatter.Format(n);

                default:
                    throw new RdfOutputException(WriterErrorMessages.UnknownNodeTypeUnserializable("Notation 3"));
            }

            return output.ToString();
        }

        /// <summary>
        /// Internal Helper method which converts a Collection into Notation 3 Syntax
        /// </summary>
        /// <param name="context">Writer Context</param>
        /// <param name="c">Collection to convert</param>
        /// <returns></returns>
        private String GenerateCollectionOutput(CompressingTurtleWriterContext context, OutputRDFCollection c)
        {
            StringBuilder output = new StringBuilder();

            if (!c.IsExplicit)
            {
                output.Append('(');

                while (c.Count > 0)
                {
                    output.Append(this.GenerateNodeOutput(context, c.Pop(), TripleSegment.Object));
                    if (c.Count > 0)
                    {
                        output.Append(' ');
                    }
                }

                output.Append(')');
            }
            else
            {
                if (c.Count == 0)
                {
                    //Empty Collection
                    //Can represent as a single Blank Node []
                    output.Append("[]");
                }
                else
                {
                    output.Append('[');

                    while (c.Count > 0)
                    {
                        output.Append(this.GenerateNodeOutput(context, c.Pop(), TripleSegment.Predicate));
                        output.Append(" ");
                        output.Append(this.GenerateNodeOutput(context, c.Pop(), TripleSegment.Object));

                        if (c.Count > 0)
                        {
                            output.AppendLine(" ; ");
                        }
                    }

                    output.Append(']');
                }
            }
            return output.ToString();
        }

        /// <summary>
        /// Helper method for generating Parser Warning Events
        /// </summary>
        /// <param name="message">Warning Message</param>
        private void RaiseWarning(String message)
        {
            RdfWriterWarning d = this.Warning;
            if (d != null)
            {
                d(message);
            }
        }

        /// <summary>
        /// Event which is raised when there is a non-fatal issue with the Graph being written
        /// </summary>
        public event RdfWriterWarning Warning;

    }
}
