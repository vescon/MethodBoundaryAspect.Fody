using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class Issue135Methods
    {
        [EmptyMethodBoundaryAspect]
        public async Task PaddedBeforeAwait(bool flag)
        {
            if (flag)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
            }

            await Task.Delay(1);

            if (flag)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
            }
        }

        [EmptyMethodBoundaryAspect]
        public async Task<int> PaddedInLoop(int count)
        {
            var sum = 0;
            for (var i = 0; i < count; i++)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                await Task.Yield();
                sum += i;
            }

            return sum;
        }


        [EmptyMethodBoundaryAspect]
        public async Task<int> EarlyReturnWithPadding(bool flag)
        {
            if (flag)
            {
                await Task.Delay(1);
                return 1;
            }

            await Task.Delay(1);
            if (!flag)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
            }

            return 2;
        }

        [EmptyMethodBoundaryAspect]
        public async Task PaddedElseAfterAwait(bool flag)
        {
            await Task.Delay(1);
            if (flag)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
            }
            else
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
            }
        }

        [EmptyMethodBoundaryAspect]
        public async Task PaddedWithFinally(bool flag)
        {
            try
            {
                await Task.Delay(1);
                if (flag)
                {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                }
            }
            finally
            {
                Nothing();
            }
        }


        // A short branch skips the block with the early return (leave.s). Widening the leave.s to leave
        // made the block 3 bytes larger, so the branch no longer fitted into a signed byte.
        [EmptyMethodBoundaryAspect]
        public async Task EarlyReturn127Bytes(bool flag)
        {
            await Task.Delay(1);
            if (flag)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                return;
            }

            Nothing();
        }

        [EmptyMethodBoundaryAspect]
        public async Task EarlyReturn126Bytes(bool flag)
        {
            await Task.Delay(1);
            if (flag)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(0); Nothing(0); Nothing(0); Nothing(0);
                return;
            }

            Nothing();
        }

        [EmptyMethodBoundaryAspect]
        public async Task EarlyReturn125Bytes(bool flag)
        {
            await Task.Delay(1);
            if (flag)
            {
                Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing(); Nothing();
                Nothing(0); Nothing(0); Nothing(0); Nothing(0); Nothing(0);
                return;
            }

            Nothing();
        }

        private static void Nothing(int value)
        {
        }

        private static void Nothing()
        {
        }
    }
}
