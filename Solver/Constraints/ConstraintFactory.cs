using Newtonsoft.Json.Linq;

namespace SudokuBlazor.Solver.Constraints
{
    public class ConstraintFactory
    {
        public static Constraint Deserialize(string json)
        {
            JObject jobject = JObject.Parse(json);
            return (string)jobject["type"] switch
            {
                "ArrowSum" => new ArrowSumConstraint(jobject),
                "DiagonalNonconsecutive" => new DiagonalNonconsecutiveConstraint(jobject),
                "KillerCage" => new KillerCageConstraint(jobject),
                "King" => new KingConstraint(jobject),
                "Knight" => new KnightConstraint(jobject),
                "LittleKiller" => new LittleKillerConstraint(jobject),
                "Nonconsecutive" => new NonconsecutiveConstraint(jobject),
                "DisjointGroups" => new DisjointGroupsConstraint(jobject),
                _ => null,
            };
        }
    }
}
