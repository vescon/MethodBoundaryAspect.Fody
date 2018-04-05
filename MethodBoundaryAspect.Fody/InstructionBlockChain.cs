using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class InstructionBlockChain
    {
        public List<InstructionBlock> InstructionBlocks { get; }

        public Instruction First => InstructionBlocks.First().First;
        public Instruction Last => InstructionBlocks.Last().Last;

        public InstructionBlockChain()
        {
            InstructionBlocks = new List<InstructionBlock>();
        }

        public void Add(InstructionBlock block)
        {
            InstructionBlocks.Add(block);
        }

        public void Add(InstructionBlockChain chain)
        {
            foreach (var block in chain.InstructionBlocks)
                Add(block);
        }

        public Instruction InsertAfter(Instruction instruction, ILProcessor processor)
        {
            var currentInstruction = instruction;
            foreach (var newInstructionBlock in InstructionBlocks)
                currentInstruction = newInstructionBlock.InsertAfter(currentInstruction, processor);

            return currentInstruction;
        }

        public void Append(ILProcessor processor)
        {
            foreach (var newInstructionBlock in InstructionBlocks)
            foreach (var instruction in newInstructionBlock.Instructions)
                processor.Append(instruction);
        }
    }
}