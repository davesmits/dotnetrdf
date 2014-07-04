/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2013 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections.Generic;
using VDS.RDF.Nodes;

namespace VDS.RDF.Graphs
{
    /// <summary>
    /// Interface for Graph Stores
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is a hybrid of the old ITripleStore and ISparqlDataset interfaces from the 1.0 APIs, it allows for data to be manipulated at either the Graph, Triple or Quad level as desired.
    /// </para>
    /// </remarks>
    public interface IGraphStore
    {
        /// <summary>
        /// Gets the names of the Graphs in the Store
        /// </summary>
        IEnumerable<INode> GraphNames
        { 
            get;
        }

        /// <summary>
        /// Gets the Graphs in the Store
        /// </summary>
        IEnumerable<IGraph> Graphs
        { 
            get;
        }

        /// <summary>
        /// Gets a specific Graph from the Store
        /// </summary>
        /// <param name="graphName">Graph name</param>
        /// <returns>A Graph if it exists in the Store, an error otherwise</returns>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        IGraph this[INode graphName]
        { 
            get; 
        }

        /// <summary>
        /// Determines whether the store has a specific graph
        /// </summary>
        /// <param name="graphName">Graph name</param>
        /// <returns>True if the Graph exists in the store, false otherwise</returns>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        bool HasGraph(INode graphName);

        /// <summary>
        /// Adds the contents of a Graph to the stores default unnamed graph
        /// </summary>
        /// <param name="g">Graph</param>
        void Add(IGraph g);

        /// <summary>
        /// Adds the contents of a Graph to the store using the given graph name for the added Quads
        /// </summary>
        /// <param name="graphName">Name of the Graph to add to</param>
        /// <param name="g">Graph</param>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        void Add(INode graphName, IGraph g);

        /// <summary>
        /// Adds a Quad to the Store
        /// </summary>
        /// <param name="q">Quad</param>
        void Add(Quad q);

        /// <summary>
        /// Copies the contents of one graph to another
        /// </summary>
        /// <param name="srcName">Source Graph name</param>
        /// <param name="destName">Target Graph name</param>
        /// <param name="overwrite">If true the contents of the target graph are overwritten, if false the copied data is added to the existing data in the target graph</param>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        void Copy(INode srcName, INode destName, bool overwrite);

        /// <summary>
        /// Moves the contents of one graph to another
        /// </summary>
        /// <param name="srcName">Source Graph name</param>
        /// <param name="destName">Destination Graph name</param>
        /// <param name="overwrite">If true the contents of the target graph are overwritten, if false the moved data is added to the existing data in the target graph</param>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        void Move(INode srcName, INode destName, bool overwrite);

        /// <summary>
        /// Clears the contents of the given graph
        /// </summary>
        /// <param name="graphName">Graph name</param>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        void Clear(INode graphName);

        /// <summary>
        /// Removes the contents of the given graph from the stores default unnamed graph
        /// </summary>
        /// <param name="g">Graph</param>
        void Remove(IGraph g);

        /// <summary>
        /// Removes the contents of the given graph from a specific graph in the store
        /// </summary>
        /// <param name="graphName">Graph name</param>
        /// <param name="g">Graph</param>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        void Remove(INode graphName, IGraph g);

        /// <summary>
        /// Removes the contents of the graph with the given URI
        /// </summary>
        /// <param name="graphName">Graph name</param>
        /// <remarks>
        /// <em>null</em> or <see cref="Quad.DefaultGraphNode"/> may be used to access the default unnamed graph
        /// </remarks>
        void Remove(INode graphName);

        /// <summary>
        /// Removes a Quad from the store
        /// </summary>
        /// <param name="q">Quad</param>
        void Remove(Quad q);

        /// <summary>
        /// Get all Quads in the store
        /// </summary>
        IEnumerable<Quad> Quads 
        {
            get;
        }

        /// <summary>
        /// Is the given Quad contained in the store?
        /// </summary>
        /// <param name="q">Quad</param>
        /// <returns></returns>
        bool Contains(Quad q);

        /// <summary>
        /// Find any quads matching the given search criteria in any graph, null is treated as a wildcard.  Implementations should not retain duplicates though it should be impossible to have duplicate quads in the first place.
        /// </summary>
        /// <param name="s">Subject</param>
        /// <param name="p">Predicate</param>
        /// <param name="o">Object</param>
        /// <returns>Enumerable of quads</returns>
        IEnumerable<Quad> Find(INode s, INode p, INode o);

        /// <summary>
        /// Finds any quads matching the given search criteria, null is treated as a wildcard.  Implementations should not retain duplicates though it should be impossible to have duplicate quads in the first place.
        /// </summary>
        /// <param name="g">Graph name</param>
        /// <param name="s">Subject</param>
        /// <param name="p">Predicate</param>
        /// <param name="o">Object</param>
        /// <returns>Enumerable of quads</returns>
        /// <remarks>
        ///  <see cref="Quad.DefaultGraphNode"/> must be used to access the default unnamed graph since <em>null</em> is treated as a wildcard for this method
        /// </remarks>
        IEnumerable<Quad> Find(INode g, INode s, INode p, INode o);
    }
}
