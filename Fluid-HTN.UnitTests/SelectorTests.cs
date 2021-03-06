﻿using System;
using System.CodeDom;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class SelectorTests
    {
        [TestMethod]
        public void AddCondition_ExpectedBehavior()
        {
            var task = new Selector() { Name = "Test" };
            var t = task.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Conditions.Count == 1);
        }

        [TestMethod]
        public void AddSubtask_ExpectedBehavior()
        {
            var task = new Selector() { Name = "Test" };
            var t = task.AddSubtask(new PrimitiveTask() {Name = "Sub-task"});

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Subtasks.Count == 1);
        }

        [TestMethod]
        public void IsValidFailsWithoutSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };

            Assert.IsFalse(task.IsValid(ctx));
        }

        [TestMethod]
        public void IsValid_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task" });

            Assert.IsTrue(task.IsValid(ctx));
        }

        [TestMethod]
        public void DecomposeWithNoSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void DecomposeWithSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
        }

        [TestMethod]
        public void DecomposeWithSubtasks2_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new Selector() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
        }

        [TestMethod]
        public void DecomposeWithSubtasks3_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
        }

        [TestMethod]
        public void DecomposeMTRFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            ctx.LastMTR.Add(0);
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.AreEqual(-1, ctx.MethodTraversalRecord[0]);
        }

        [TestMethod]
        public void DecomposeDebugMTRFails_ExpectedBehavior()
        {
            var ctx = new MyDebugContext();
            ctx.Init();

            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            ctx.LastMTR.Add(0);
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MTRDebug.Count == 1);
            Assert.IsTrue(ctx.MTRDebug[0].Contains("REPLAN FAIL"));
            Assert.IsTrue(ctx.MTRDebug[0].Contains("Sub-task2"));
        }

        [TestMethod]
        public void DecomposeMTRSucceedsWhenEqual_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            ctx.LastMTR.Add(1);
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 0);
            Assert.IsTrue(plan.Count == 1);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskSucceeds_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2"};
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task3", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 0);
        }

        [TestMethod]
        public void DecomposeNestedCompoundSubtaskFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task4", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskBeatsLastMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            ctx.LastMTR.Add(1);
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskEqualToLastMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            ctx.LastMTR.Add(0);
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskLoseToLastMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(task2);

            ctx.LastMTR.Add(0);
            var plan = task.Decompose(ctx, 0);

            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == -1);
        }
    }
}
