using System;

namespace AnalysisProject
{
    public class Class1
    {
        void M(bool flag)
        {
            object o1 = new object(/*0*/);
            object o2 = new object(/*1*/);
            if (flag)
            {
                o1 = new object(/*2*/);
            }

            Console.WriteLine(o1);

            if (flag)
            {
                o1 = /*3*/ o2;
                o2 = new object(/*4*/);
            }

            o2 = new object(/*5*/);

            if (flag)
            {
                Console.WriteLine(o2);
            }
        }
    }
}
