using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Broadcaster : MonoBehaviour {

	public enum Arithmetic { None, Add, Subtract, Multiply, Divide };
    public enum BroadcastType { MatchValue, AddValue };

    public bool update;
    public BroadcastType broadcastType;
    public Arithmetic arithmetic;

    void Update()
    {
        if (update)
        {
            if (arithmetic == Arithmetic.None)
            {
                UpdateValue();
            }

            else
            {
                ArithmeticDispatch();
                UpdateValue();
            }
        }
    }
    protected virtual void UpdateValue() { }

    protected void ArithmeticDispatch()
    {
        switch(arithmetic) {
            case Arithmetic.Add:
                AddOperator();
                break;
            case Arithmetic.Subtract:
                SubtractOperator();
                break;
            case Arithmetic.Multiply:
                MultiplyOperator();
                break;
            case Arithmetic.Divide:
                DivideOperator();
                break;
            default:
                break;
        }
    }

    protected virtual void AddOperator() { }
    protected virtual void SubtractOperator() { }
    protected virtual void MultiplyOperator() { }
    protected virtual void DivideOperator() { }


    
}
