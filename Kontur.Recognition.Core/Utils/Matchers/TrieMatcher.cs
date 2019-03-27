using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Matchers
{
	/// <summary>
	/// This class implements matching automata based on prefix tree (trie) (see Aho–Corasick string matching algorithm).
	/// The automata is built in the following way. Each state of automata corresponds to some
	/// prefix of some word of recognized language (the reversed prefix can be read by traversing parents of nodes
	/// up to root node). States are indexed with integer numbers. Each node (TrieNode) knows it's Id, Id of parent node 
	/// (corresponds to prefix which is shorter by one character), the corresponding character (the last character in the word represented by the node), 
	/// and Id of the node representing maximal prefix which is a strict suffix of the word represented by the node. 
	/// 
	/// Transitions between states are kept in separate dictionary. Transition is encoded in form
	/// (nodeId, char) -> (nextNodeId, nextTransitionChar)
	/// here nodeId is the current state of the automata, char is the observed character, nextNodeId is the next state of the automata,
	/// nextTransitionChar is the reference to the next known transition rule for the same nodeId and observed characer nextTransitionChar 
	/// (this way all the rules for the same nodeId make a lilnked list linked via nextTransitionChar; top of the list is kept
	/// in member firstOutgointChar of the node. Value of \xFFFF represent end of the list). This approach allows to efficiently enumerate 
	/// edges of automata.
	/// </summary>
	public class TrieMatcher
	{
		private readonly List<TrieNode> nodes = new List<TrieNode>();
		private readonly Dictionary<TransitionKey, TransitionTarget> transitions = new Dictionary<TransitionKey, TransitionTarget>();
		private readonly TrieNode rootNode;
		private bool prefixTableIsDirty = true;

		internal const char NullRef = '\xFFFF';

		/// <summary>
		/// Creates new matcher with empty dictionary
		/// </summary>
		public TrieMatcher()
		{
			rootNode = MakeRootNode();
		}

		public int NodesCount { get { return nodes.Count; } }

		private TrieNode MakeRootNode()
		{
			return AddNodeImpl(0, NullRef);
		}

		private TrieNode AddNode(TrieNode parentNode, char nodeChar)
		{
			return AddNodeImpl(parentNode.NodeId, nodeChar);
		}

		private TrieNode AddNodeImpl(int parentNodeId, char nodeChar)
		{
			var nodeId = nodes.Count;
			var result = new TrieNode(nodeId, nodeChar, parentNodeId);
			nodes.Add(result);
			return result;
		}

		private TrieNode GetOrCreateNextState(TrieNode node, char c)
		{
			TransitionTarget transitionTarget;
			var transition = new TransitionKey(node.NodeId, c);
			if (!transitions.TryGetValue(transition, out transitionTarget))
			{
				var newNode = AddNode(node, c);
				// Creating new outgoing edge and store character corresponding to 
				// existing outgoing edge (this way outgoing edges for single vertex form a linked list)
				transitionTarget = new TransitionTarget(newNode.NodeId, node.FirstOutgointChar);
				// Updating reference to top of linked list of 
				node.UpdateOutgoingChar(c);
				transitions.Add(transition, transitionTarget);
				return newNode;
			}
			return nodes[transitionTarget.NextNodeId];
		}

		[CanBeNull]
		private TrieNode GetNextState(TrieNode node, char c)
		{
			TransitionTarget transitionTarget;
			var transition = new TransitionKey(node.NodeId, c);
			if (transitions.TryGetValue(transition, out transitionTarget))
			{
				return nodes[transitionTarget.NextNodeId];
			}
			foreach (var n in TraversePrefixes(node))
			{
				transition = new TransitionKey(n.NodeId, c);
				if (transitions.TryGetValue(transition, out transitionTarget))
				{
					return nodes[transitionTarget.NextNodeId];
				}
			}
			return null;
		}

		public void AddString(string value)
		{
			var currentNode = GetRoot();
			currentNode = value.Aggregate(currentNode, GetOrCreateNextState);
			currentNode.SetTerminal();
			prefixTableIsDirty = true;
		}

		public void AddAll(IEnumerable<string> aliases)
		{
			foreach (var val in aliases)
			{
				AddString(val);
			}
		}

		private TrieNode GetRoot()
		{
			return nodes[0];
		}

		internal string GetWord(TrieNode node)
		{
			var result = new StringBuilder();
			GetPrefixImpl(node, result);
			return result.ToString();
		}

		private void GetPrefixImpl(TrieNode node, StringBuilder result)
		{
			if (node == rootNode)
			{
				return;
			}
			GetPrefixImpl(nodes[node.ParentNodeId], result);
			result.Append(node.NodeChar);
		}

		public IEnumerable<string> Words()
		{
			var buffer = new StringBuilder();
			return WordsImpl(rootNode, buffer);
		}

		private IEnumerable<string> WordsImpl(TrieNode node, StringBuilder buffer)
		{
			var currentNode = node;
			var currentTransisionChar = currentNode.FirstOutgointChar;
			TransitionTarget transitionTarget;
			while (transitions.TryGetValue(
				new TransitionKey(currentNode.NodeId, currentTransisionChar), out transitionTarget))
			{
				buffer.Append(currentTransisionChar);
				foreach (var w in WordsImpl(nodes[transitionTarget.NextNodeId], buffer))
				{
					yield return w;
				}
				buffer.Length--;
				currentTransisionChar = transitionTarget.NextTransitionChar;
			}
			if (node.IsTerminal)
			{
				yield return buffer.ToString();
			}
		}

		/// <summary>
		/// Internal helper to traverse subtree (starting from specified node) in breadth-first order.
		/// Given action is invoked for each node being traversed
		/// </summary>
		/// <param name="startNode"></param>
		/// <param name="processNode"></param>
		private void TraverseNodesBFS(TrieNode startNode, Action<TrieNode> processNode)
		{
			var nodesStack = new Queue<TrieNode>(NodesCount);
			nodesStack.Enqueue(startNode);
			while (nodesStack.Count > 0)
			{
				var currentNode = nodesStack.Dequeue();
				processNode(currentNode);
				var currentTransisionChar = currentNode.FirstOutgointChar;
				TransitionTarget transitionTarget;
				while (transitions.TryGetValue(
					new TransitionKey(currentNode.NodeId, currentTransisionChar), out transitionTarget))
				{
					nodesStack.Enqueue(nodes[transitionTarget.NextNodeId]);
					currentTransisionChar = transitionTarget.NextTransitionChar;
				}
			}
		}

		/// <summary>
		/// For given node there is a string which is represented by this node in a set of prefixes of dictionary.
		/// This method calculates maximal suffix of this string which at the same time is a maximal prefix of some word in a dictionary
		/// </summary>
		/// <param name="node"></param>
		private void CalculateMaxSuffixMaxPrefix(TrieNode node)
		{
			if (node == rootNode)
			{
				return;
			}
			var parentNode = nodes[node.ParentNodeId];
			foreach (var currentNode in TraversePrefixes(parentNode))
			{
				TransitionTarget transitionTarget;
				if (transitions.TryGetValue(new TransitionKey(currentNode.NodeId, node.NodeChar), out transitionTarget))
				{
					var targetNode = nodes[transitionTarget.NextNodeId];
					node.MaxPrefixNodeId = targetNode.NodeId;
					node.HasTerminalSuffix = targetNode.HasTerminalSuffix || targetNode.IsTerminal;
					return;
				}
			}
			// No suffix of node's word is present as a prefix of trie
			node.MaxPrefixNodeId = 0;
			node.HasTerminalSuffix = false;
		}

		/// <summary>
		/// Internal helper to traverse nodes representing prefixes of given word (specified by node)
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		internal IEnumerable<TrieNode> TraversePrefixes(TrieNode node)
		{
			while (node != rootNode)
			{
				var nextNode = nodes[node.MaxPrefixNodeId];
				yield return nextNode;
				node = nextNode;
			}
		}

		private void RecalculatePrefixesTable()
		{
			if (prefixTableIsDirty)
			{
				TraverseNodesBFS(rootNode, CalculateMaxSuffixMaxPrefix);
				prefixTableIsDirty = false;
			}
		}

		internal TrieNode Read(string value)
		{
			RecalculatePrefixesTable();
			var currentNode = rootNode;
			foreach (var c in value)
			{
				currentNode = GetNextState(currentNode, c) ?? rootNode;
			}
			return currentNode;
		}

		/// <summary>
		/// Attempts to locate words from loaded dictionary in given text. Found matches may overlap!
		/// For example, if there are two words in a dictionary: "word" and "or" then when matching is done 
		/// against string "word" two matches will be found: one for "word" starting from 0 and another one for "or" starting from 2.
		/// </summary>
		/// <param name="value">The string to process</param>
		/// <returns>Found matches (matching is performed lazily)</returns>
		public IEnumerable<TrieMatch> LocateMatches(string value)
		{
			RecalculatePrefixesTable();
			var currentNode = rootNode;
			var pos = 0;
			foreach (var c in value)
			{
				pos++;
				currentNode = GetNextState(currentNode, c) ?? rootNode;
				if (currentNode.IsTerminal)
				{
					var word = GetWord(currentNode);
					yield return new TrieMatch(pos - word.Length, word);
				}
				if (currentNode.HasTerminalSuffix)
				{
					// This block allows to locate matches which are substrings of longer match
					foreach (var n in TraversePrefixes(currentNode))
					{
						if (n.IsTerminal)
						{
							var word = GetWord(n);
							yield return new TrieMatch(pos - word.Length, word);
						}
					}
				}
			}
		}

		private struct TransitionKey : IEquatable<TransitionKey>
		{
			private readonly int nodeId;
			private readonly char nextChar;

			public TransitionKey(int nodeId, char nextChar)
			{
				this.nodeId = nodeId;
				this.nextChar = nextChar;
			}

			public bool Equals(TransitionKey other)
			{
				return nodeId == other.nodeId && nextChar == other.nextChar;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is TransitionKey && Equals((TransitionKey)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (nodeId * 397) ^ nextChar.GetHashCode();
				}
			}
		}

		private struct TransitionTarget
		{
			public readonly int NextNodeId;

			public readonly char NextTransitionChar;

			public TransitionTarget(int nextNodeId, char nextTransitionChar)
				: this()
			{
				NextNodeId = nextNodeId;
				NextTransitionChar = nextTransitionChar;
			}
		}
	}

	internal class TrieNode
	{
		private readonly int nodeId;
		public int NodeId { get { return nodeId; } }

		private readonly char nodeChar;
		public char NodeChar { get { return nodeChar; } }

		private readonly int parentNodeId;
		public int ParentNodeId { get { return parentNodeId; } }

		private bool isTerminal;
		public bool IsTerminal { get { return isTerminal; } }

		// Id of node which represents the maximal prefix of trie which is a strict suffix of the word represented by this node
		internal int MaxPrefixNodeId { get; set; }

		internal bool HasTerminalSuffix { get; set; }

		private char firstOutgointChar = TrieMatcher.NullRef;
		public char FirstOutgointChar { get { return firstOutgointChar; } }

		public TrieNode(int nodeId, char nodeChar, int parentNodeId)
		{
			this.nodeId = nodeId;
			this.nodeChar = nodeChar;
			this.parentNodeId = parentNodeId;
		}

		public void SetTerminal()
		{
			isTerminal = true;
		}

		public void UpdateOutgoingChar(char c)
		{
			firstOutgointChar = c;
		}
	}

}