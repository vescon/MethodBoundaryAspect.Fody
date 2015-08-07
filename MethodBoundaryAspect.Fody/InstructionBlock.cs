using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class InstructionBlock
    {
        public string Name { get; set; }
        public Instruction[] Instructions { get; private set; }

        public Instruction First
        {
            get { return Instructions.First(); }
        }

        public Instruction Last
        {
            get { return Instructions.Last(); }
        }

        public InstructionBlock(string name, List<Instruction> instructions)
            :this(name, instructions.ToArray())
        {
        }

        public InstructionBlock(string name, params Instruction[] instructions)
        {
            Name = name;
            Instructions = instructions;
        }

        public Instruction InsertAfter(Instruction instruction, ILProcessor processor)
        {
            var currentInstruction = instruction;
            foreach (var newInstruction in Instructions)
            {
                processor.InsertAfter(currentInstruction, newInstruction);
                currentInstruction = newInstruction;
            }

            return currentInstruction;
        }
    }
}