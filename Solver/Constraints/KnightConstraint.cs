﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static SudokuBlazor.Solver.SolverUtility;

namespace SudokuBlazor.Solver.Constraints
{
    public class KnightConstraint : Constraint
    {
        public KnightConstraint() { }

        public KnightConstraint(JObject _) { }

        public override string Serialized => new JObject()
        {
            ["type"] = "Knight",
            ["v"] = 1,
        }.ToString();

        public override string Name => "Anti-Knight";

        public override string Icon => "M 11.9975,0 C 10.364124,0 9.282418,0.9086159 8.853342,1.8460768 8.528831,2.5563837 8.568493,3.0503541 8.564887,3.4613943 7.876201,3.9301251 7.151458,4.5286575 6.5745486,5.1055565 5.2837136,6.3963686 4.5769991,8.2352337 4.6130555,10.182267 c 0.018028,0.8978 0.2019184,1.799204 0.5480642,2.711426 0.173073,0.457915 0.4903733,1.25115 0.7788282,2.134527 0.2884548,0.883376 0.5192187,1.856895 0.5192187,2.509511 H 5.536111 c -0.028845,0 -0.057691,0 -0.086536,0 -0.4975841,0.03245 -0.8797873,0.454308 -0.8617591,0.951884 0.014423,0.497575 0.4218659,0.890587 0.9194501,0.894192 L 3.8630729,21.604868 3.69,21.835628 V 23.999 h 16.615 v -2.163372 l -0.173073,-0.23076 -1.673038,-2.221061 c 0.331723,0.0036 0.641812,-0.169464 0.811279,-0.457914 0.165861,-0.288449 0.165861,-0.641799 0,-0.930249 -0.169467,-0.288449 -0.479556,-0.461518 -0.811279,-0.457913 h -0.923056 c 0,-2.087654 -1.298047,-4.002238 -2.509558,-5.480541 -0.692291,-0.847322 -0.919449,-0.995151 -1.384583,-1.442248 0.165863,-0.09736 0.371387,-0.212732 0.490374,-0.288449 0.147833,-0.09375 0.227158,-0.151436 0.259609,-0.17307 0.259609,0 0.277638,0.04326 0.548064,0.259605 0.270427,0.216337 0.822096,0.663433 1.586502,0.663433 0.670658,0 1.161032,-0.432674 1.413429,-0.749968 0.230763,-0.292056 0.356963,-0.5119975 0.374991,-0.5480548 C 18.397591,9.7387789 18.577876,9.68108 18.805035,9.518834 19.032192,9.356581 19.327859,8.9960197 19.381944,8.5957961 19.436034,8.1955715 19.309824,7.8458269 19.15118,7.4996873 18.916811,6.9876901 18.480523,6.399974 17.795442,5.6824555 17.110363,4.9649372 16.212546,4.2041522 15.112812,3.7498437 15.029881,3.713788 15.09839,3.7678716 15.055122,3.7209988 15.011853,3.674126 14.882049,3.4794221 14.622439,3.2883245 14.283505,3.0395371 13.547946,3.010692 12.920555,2.9421851 V 0 Z M 11.074444,2.0479915 V 4.6151923 H 11.9975 c 1.258384,0 1.536022,0.1514364 1.52881,0.1442248 -0.0036,-0.00361 0.0036,0.014423 0.144228,0.1730697 0.140621,0.1586473 0.414654,0.4074348 0.749982,0.5480541 0.739166,0.3064774 1.478332,0.9050105 2.04803,1.4999374 0.533642,0.5552654 0.890604,1.1069262 0.951901,1.2114882 -0.147833,0.072119 -0.302877,0.1225908 -0.490373,0.3461396 -0.252398,0.3064774 -0.342541,0.5192084 -0.432683,0.6345883 -0.02885,0.036059 -0.02524,0.046869 -0.02885,0.057689 -0.07933,-0.01082 -0.13341,-0.061299 -0.374991,-0.2596042 -0.302877,-0.2451814 -0.905027,-0.6634337 -1.701884,-0.6634337 -0.605755,0 -0.948295,0.2776319 -1.240356,0.4615195 -0.29206,0.1838866 -0.526429,0.3172931 -0.605755,0.3461396 l -0.02885,0.028849 h -0.02885 c -0.169467,0.072119 -0.461528,0.086538 -0.461528,0.086538 L 11.07443,9.2015426 v 1.4710924 l 0.317301,0.259605 c 0,0 1.096128,0.966305 2.192257,2.307596 1.09614,1.34129 2.105732,3.068381 2.105732,4.297898 H 8.305276 c 0,-1.034812 -0.288455,-2.120104 -0.605756,-3.08641 C 7.38222,13.485018 7.007229,12.641303 6.863001,12.259107 6.5925767,11.545192 6.4735893,10.80604 6.4591666,10.124577 6.4303211,8.6426692 6.960357,7.3158007 7.872595,6.4035794 8.474745,5.8014408 9.37256,5.0478665 9.862934,4.7594171 L 10.324461,4.4998125 V 3.9806034 c 0,-0.3425341 0,-0.9158268 0.201919,-1.3557128 0.115382,-0.2487875 0.274032,-0.439885 0.548064,-0.5768991 z M 7.84375,19.383807 h 8.307499 l 2.076876,2.769116 H 5.766875 Z";

        public override string Rules => "Cells which are a chess knight's move apart cannot contain the same digit.";

        public override bool MarkConflicts(int[] values, bool[] conflicts)
        {
            bool conflict = false;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int cellIndex = FlatIndex((i, j));
                    if (conflicts[cellIndex])
                    {
                        continue;
                    }
                    int val = values[cellIndex];
                    if (val == 0)
                    {
                        continue;
                    }

                    if (i - 2 >= 0 && j - 1 >= 0)
                    {
                        int otherCellIndex = FlatIndex((i - 2, j - 1));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                    if (i - 2 >= 0 && j + 1 < WIDTH)
                    {
                        int otherCellIndex = FlatIndex((i - 2, j + 1));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                    if (i - 1 >= 0 && j - 2 >= 0)
                    {
                        int otherCellIndex = FlatIndex((i - 1, j - 2));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                    if (i - 1 >= 0 && j + 2 < WIDTH)
                    {
                        int otherCellIndex = FlatIndex((i - 1, j + 2));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                    if (i + 2 < HEIGHT && j - 1 >= 0)
                    {
                        int otherCellIndex = FlatIndex((i + 2, j - 1));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                    if (i + 2 < HEIGHT && j + 1 < WIDTH)
                    {
                        int otherCellIndex = FlatIndex((i + 2, j + 1));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                    if (i + 1 < HEIGHT && j - 2 >= 0)
                    {
                        int otherCellIndex = FlatIndex((i + 1, j - 2));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                    if (i + 1 < HEIGHT && j + 2 < WIDTH)
                    {
                        int otherCellIndex = FlatIndex((i + 1, j + 2));
                        if (val == values[otherCellIndex])
                        {
                            conflicts[cellIndex] = true;
                            conflicts[otherCellIndex] = true;
                            conflict = true;
                        }
                    }
                }
            }
            return conflict;
        }

        public override bool EnforceConstraint(SudokuSolver sudokuSolver, int i, int j, int val)
        {
            if (i - 2 >= 0 && j - 1 >= 0)
            {
                if (!sudokuSolver.ClearValue(i - 2, j - 1, val))
                {
                    return false;
                }
            }
            if (i - 2 >= 0 && j + 1 < WIDTH)
            {
                if (!sudokuSolver.ClearValue(i - 2, j + 1, val))
                {
                    return false;
                }
            }
            if (i - 1 >= 0 && j - 2 >= 0)
            {
                if (!sudokuSolver.ClearValue(i - 1, j - 2, val))
                {
                    return false;
                }
            }
            if (i - 1 >= 0 && j + 2 < WIDTH)
            {
                if (!sudokuSolver.ClearValue(i - 1, j + 2, val))
                {
                    return false;
                }
            }
            if (i + 2 < HEIGHT && j - 1 >= 0)
            {
                if (!sudokuSolver.ClearValue(i + 2, j - 1, val))
                {
                    return false;
                }
            }
            if (i + 2 < HEIGHT && j + 1 < WIDTH)
            {
                if (!sudokuSolver.ClearValue(i + 2, j + 1, val))
                {
                    return false;
                }
            }
            if (i + 1 < HEIGHT && j - 2 >= 0)
            {
                if (!sudokuSolver.ClearValue(i + 1, j - 2, val))
                {
                    return false;
                }
            }
            if (i + 1 < HEIGHT && j + 2 < WIDTH)
            {
                if (!sudokuSolver.ClearValue(i + 1, j + 2, val))
                {
                    return false;
                }
            }
            return true;
        }

        public override LogicResult StepLogic(SudokuSolver sudokuSolver, StringBuilder logicalStepDescription, bool isBruteForcing)
        {
            return LogicResult.None;
        }
    }
}
