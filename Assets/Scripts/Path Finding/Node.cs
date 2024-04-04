using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node{

    // Constants: 
    const int MAX_CONNECTIONS = 8; 

    // Variables:
    private Node[] connections = new Node[MAX_CONNECTIONS];
    private float weight = 0.0f;

    // Parameters:
    public float Weight{ get { return weight; } set{weight = Mathf.Clamp(value, -1.0f, 0.0f); } }
    public Node BestConnection { get; private set; }

    public Node(Node[] connections, float weight){
        this.connections = connections;
        Weight = weight;
    }

    private void FindBestNode(){
        
        // Temp node to store the best node found yet.
        Node candidateNode = connections[0];

        // Iterate through each node, find the one with the highest weight.
        foreach(Node node in connections){
            
            if(node == null){
                continue;
            }

            if (node.Weight > candidateNode.Weight){
                candidateNode = node;
            }
        }
        // Set the best node.
        BestConnection = candidateNode;
    }
    
    public void RemoveNode(int index){
        /* Removes the node at a given index. */
        connections[index] = null;
        FindBestNode();
    }

    public int AddNode(Node newNode){
        /* Add a new node at the first available position. */

        int index = 0;

        // Insert a node at the first open node.
        foreach(Node node in connections){
            if (node == null){
                connections[index] = newNode;
                FindBestNode();
                return index;
            }
            index++;
        }

        Debug.LogError("Node limit reached! Could not add a new node.");
        return -1;
    }
}