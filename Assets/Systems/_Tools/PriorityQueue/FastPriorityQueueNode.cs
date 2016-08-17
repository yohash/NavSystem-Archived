namespace Priority_Queue
{
	public class FastPriorityQueueNode
	{
		/// The Priority to insert this node at.  Must be set BEFORE adding a node to the queue
		public double Priority { get; set; }

		/// <b>Used by the priority queue - do not edit this value.</b>
		/// Represents the order the node was inserted in
		public long InsertionIndex { get; set; }

		/// <b>Used by the priority queue - do not edit this value.</b>
		/// Represents the current position in the queue
		public int QueueIndex { get; set; }
	}
}