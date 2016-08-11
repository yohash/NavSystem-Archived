using System.Collections.Generic;

namespace Priority_Queue
{
	/// The IPriorityQueue interface.  This is mainly here for purists, and in case I decide to add more implementations later.
	/// For speed purposes, it is actually recommended that you *don't* access the priority queue through this interface, since the JIT can
	/// (theoretically?) optimize method calls from concrete-types slightly better.
	public interface IPriorityQueue<T> : IEnumerable<T>
	{
		/// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken by first-in-first-out.
		/// See implementation for how duplicates are handled.
		void Enqueue(T node, double priority);

		/// Removes the head of the queue (node with minimum priority; ties are broken by order of insertion), 
		/// and returns it.
		T Dequeue();

		/// Removes every node from the queue.
		void Clear();

		/// Returns whether the given node is in the queue.
		bool Contains(T node);

		/// Removes a node from the queue.  The node does not need to be the head of the queue.  
		void Remove(T node);

		/// Call this method to change the priority of a node.  
		void UpdatePriority(T node, double priority);

		/// Returns the head of the queue, without removing it (use Dequeue() for that).
		T First { get; }

		/// Returns the number of nodes in the queue.
		int Count { get; }
	}
}