1. I’ll begin the presentation with a few very basic definitions for analysis.

2. I’ll move onto a DFA example on Reaching Definitions Analysis.

3. Then I’ll cover a few slides which gives a very quick refresher on the mathematical basics and terminology of DFA theory.

4. This should help lay the groundwork to walk through the DFA datatypes in the roslyn-analyzers repo. I have one slide for every important data type. Every slide has a hyperlink to the actual code for the datatype. For every slide, I’ll first talk about the datatype and then click on this link to bring up the code and walk you through important parts of the code for that datatype. You’ll see how bunch of these datatypes directly map to the theoretical concepts.

5. Then we will move onto the main part of the session. I’ll walk you through the high-level steps on how to implement a custom Dataflow analysis in roslyn-analyzers repo. For every step, I’ll show the actual implementation of Reaching Definitions Analysis in the analyzers repo and walk you through the important parts of the code.

6. Finally, I’ll show you a demo of this Reaching Definition Analysis in action with a console application that for a given test code, computes and displays reaching definitions for every basic block in the CFG.

7. Hopefully, all of this will give you enough background so that if you are planning to implement your own custom dataflow analysis for your research or course work, you should be able to do so in our repo.

