CalculateSuccessChance: function(c) {
   var n = (this.DEBUG && !c) ? true : false;
   if (n) {
      console.log("----- Start ComputeSuccessChance -----")
   }
   var s = [];  // Follower information
   if (c) {
      s = c
   } else {
      for (var w in this.followers) {
         if (this.followers[w].hasOwnProperty("follower") && this.followers[w].follower.id) {
            s.push({
               follower: this.followers[w].follower.id,
               abilities: this.followers[w].chosenAbilities ? this.followers[w].chosenAbilities.slice() : [],
               avgilvl: this.followers[w].chosenItemLevel ? parseInt(this.followers[w].chosenItemLevel) : this.followers[w].follower.itemlevel,
               level: this.followers[w].chosenLevel ? parseInt(this.followers[w].chosenLevel) : this.followers[w].follower.level,
               quality: this.followers[w].chosenQuality ? parseInt(this.followers[w].chosenQuality) : this.folìowers[w].follower.quality
            })
         }
      }
   }
   var d = [];  // mission mechanics (abilities that the bosses have)
   if (this.mission && this.mission.encounters) {
      for (w in this.mission.encounters) {
         if (this.mission.encounters[w].mechanics) {
            for (var v in this.mission.encounters[w].mechanics) {
               d.push(this.mission.encounters[w].mechanics[v])
            }
         }
      }
   }
   var o = this.GetMentorInfo(s);   // mentor information
   var r;
    // Loop all follower objects
   for (w = 0; w < s.length; ++w) {
      r = s[w]; // current follower
       // If mentor level > current follower level
      if (o.level > r.level) {
         if (n) {
            console.log("Mentored follower %d from level %d to level %d.", r.follower, r.level, o.level)
         }
         r.level = o.level
      }
       // If mentor itemlevel > current follower itemlevel
      if (o.itemlevel > r.avgilvl) {
         if (n) {
            console.log("Mentored follower %d from item level %d to item level %d.", r.follower, r.avgilvl, o.itemlevel)
         }
         r.avgilvl = o.itemlevel
      }
      r.bias = $WH.fround(this.GetFollowerBias(r.level, r.avgilvl));
      if (n) {
         console.log("Follower %d bias: %.2f", r.follower, r.bias)
      }
   }
    //this.mission.numFollowers in c#
   var D = this.mission.followers * 100;
   var B = D;
    // This code calculates some sort of coefficient called D
    // d = <missionmechanics> object
    // <missionmechanics>.length > 0
   if (d.length > 0) {
       // Loop all mission mechanics
      for (w = 0; w < d.length; ++w) {
         var C = d[w];  // current mechanic
          // If it's not "Abilities"
          // 0 = Environments
          // 1 = Races
          // 2 = Abilities
         if (C.category != 2) {
            D = B
         } else {
            D = B + C.amount;
            B += C.amount
         }
      }
   }
   if (D <= 0) {
      return {
         chance: 100,
         chanceOver: 0
      }
   }
   var b = $WH.fround(100 / D);
   if (n) {
      console.log("coeff: ", b)
   }
   var l = 0;   // running amount of success chance
    // Loop all follower objects
   for (w = 0; w < s.length; ++w) {
      r = s[w];
      var z = $WH.fround(this.CalcChance(100, 150, r.bias) * b);
      l += z;
      if (n) {
         console.log("Added %.2f to success due to follower %d bias.", z, r.follower)
      }
   }
   var A = 0;
   var f = 0;
   this.registeredThreatCounters = {};  // registeredThreatCounters array, initialize
    // d = <missionmechanics> object
    // <missionmechanics>.length > 0
   if (d.length > 0) {
      do {
         C = d[A];  // Current mission mechanic
          // 0 = Environments
          // 1 = Races
          // 2 = Abilities
          // If no category or the category == Abilities
         if (!C.category || C.category == 2) {
            var m = C.amount;   // amount for this mechanic
             // Only if the mission has follower slots
            if (this.mission.followers > 0) {
                // Loop all follower objects
               for (w = 0; w < s.length; ++w) {
                  r = s[w]; // Current follower object
                   // Loop current follower's abilities
                  for (v = 0; v < r.abilities.length; ++v) {
                     var F = g_garrison_abilities[r.abilities[v]];  // Current ability object
                     var e = F.type.length; // number of types of this ability
                      // Loop all the types of this ability
                     for (var E = 0; E < e; ++E) {
                         // If the current ability counters this mission mechanic
                         // AND
                         // the low order bit of amount1 for this ability is NOT set
                         // AND
                         // the amount for this mechanic is greater than 0
                         // AND
                         // we haven't already countered this threat with a different follower
                         // w = index for follower objects
                         // v = index for current follower ability
                         // E = index for current Type of this ability
                        if (C.type == F.counters[E] && !(F.amount1[E] & 1) && m > 0 && !this.ThreatCounterIsAlreadyRegistered(w, v, E)) {
                           var q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                           var a = C.amount;
                           if (q <= a) {
                              a = q
                           }
                           this.RegisterThreatCounter(w, v, E);
                            // reduce mechanic amount by amount countered
                           m = m - a
                        }
                     }
                  }
               }
            } // if (this.mission.followers > 0) // mission has follower slots
             // Insure mechanic amount is not negative
            if (m < 0) {
               m = 0
            }
             // Calculate success based on how much of the mechanic was countered
            f = $WH.fround((C.amount - m) * b);
            l += f;
            if (n) {
               console.log("Added %.2f to success due to followers countering boss mechanic %d.", f, C.id)
            }
         }++A   // Increment A - Also end of if statement - // If no category or the category == Abilities - if (!C.category || C.category == 2) {
      } while (A < d.length)    // Loop while mechanics remain
   } // if (d.length > 0) { - If mission has mechanics to counter
    // Loop all mission mechanics
   for (A = 0; A < d.length; ++A) {
      C = d[A]; // current mission mechanic
       // Category == Races
      if (C.category == 1) {
          // Only if the mission has follower slots
         if (this.mission.followers > 0) {
             // Loop all follower objects
            for (w = 0; w < s.length; ++w) {
               r = s[w];    // current follower
                // Loop current follower's abilities
               for (v = 0; v < r.abilities.length; ++v) {
                  F = g_garrison_abilities[r.abilities[v]];  // Current ability object
                  e = F.type.length; // number of types of this ability
                   // Loop all the types of this ability
                  for (E = 0; E < e; ++E) {
                      // If the current ability counters this mission mechanic
                     if (C.type == F.counters[E]) {
                        q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                        q *= b; // multiply by coefficient calculated earlier
                        q = $WH.fround(q);
                        l += q;
                        if (n) {
                           console.log("Added %.2f to success due to follower %d enemy race ability %d.", q, r.follower, C.id)
                        }
                     }
                  }
               }
            }
         }
      }
   } // Loop all mission mechanics - for (A = 0; A < d.length; ++A) {
    // Only if the mission has follower slots
   if (this.mission.followers > 0) {
       // Loop all followers
      for (w = 0; w < s.length; ++w) {
         r = s[w];  // current follower
          // Loop all follower abilities
         for (v = 0; v < r.abilities.length; ++v) {
            F = g_garrison_abilities[r.abilities[v]];  // Current ability object
            e = F.type.length; // number of types of this ability
             // Loop all the types of this ability
            for (E = 0; E < e; ++E) {
                // If the ability counter exists for this type
                // AND
                // If it counters the mission mechanictype
               if (F.counters[E] && F.counters[E] == this.mission.mechanictype) {
                  q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                  q *= b;
                  q = $WH.fround(q);
                  l += q;
                  if (n) {
                     console.log("Added %.2f to success due to follower %d environment ability %d.", q, r.follower, F.id)
                  }
               }
            }
         }
      }
   }
   var y = this.GetMissionTimes(s);
   if (this.mission.followers > 0) {
      for (w = 0; w < s.length; ++w) {
         r = s[w];
         for (v = 0; v < r.abilities.length; ++v) {
            F = g_garrison_abilities[r.abilities[v]];
            e = F.type.length;
            for (E = 0; E < e; ++E) {
               var u = false;
               switch (F.type[E]) {
                  case 1:
                     if (s.length == 1) {
                        u = true
                     }
                     break;
                  case 2:
                     u = true;
                     break;
                  case 5:
                     if (this.CheckEffectRace.call(this, s, F.race[E], w)) {
                        u = true
                     }
                     break;
                  case 6:
                     if (y.missiontime > 3600 * F.hours[E]) {
                        u = true
                     }
                     break;
                  case 7:
                     if (y.missiontime < 3600 * F.hours[E]) {
                        u = true
                     }
                     break;
                  case 9:
                     if (y.traveltime > 3600 * F.hours[E]) {
                        u = true
                     }
                     break;
                  case 10:
                     if (y.traveltime < 3600 * F.hours[E]) {
                        u = true
                     }
                     break;
                  default:
                     break
               }
               if (u) {
                  q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                  q *= b;
                  q = $WH.fround(q);
                  l += q;
                  if (n) {
                     console.log("Added %.2f to success due to follower %d trait %d.", q, r.follower, F.type[E])
                  }
               }
            }
         }
      }
   }
   if (n) {
      console.log("Total before adding base chance: %.2f", l)
   }
   var t = true;
   var k = 100;
   var h;
   var p = (((100 - this.mission.basebonuschance) * l) * 0.01) + this.mission.basebonuschance;
   if (n) {
      console.log("Total after base chance: %.2f", p)
   }
   h = p;
   var g = h;
   if (t && k <= p) {
      h = k
   }
   if (n) {
      if (t && g > 100) {
         console.log("Total success chance: %.2f, (%.2f before clamping).", h, g)
      } else {
         console.log("Total success chance: %.2f.", h)
      }
      console.log("----- End ComputeSuccessChance -----")
   }
   return {
      chance: Math.floor(h),
      chanceOver: g - h
   }
}

// =============================================================
// This function appears to always return {level: 0, itemlevel: 0}
GetMentorInfo: function(h) {
    //h is an object containing follower information
    // follower:
    // abilities:
    // avgilvl:
    // level:
    // quality:
   var f = 0; // return: level
   var a = 0; // return: itemlevel
   if (this.mission.followers > 0) {
       // Loop all passed in follower objects
      for (var d = 0; d < h.length; ++d) {
         var e = h[d];  // current follower
          // Loop all follower abilities
         for (var c = 0; c < e.abilities.length; ++c) {
            //g_garrison_abilities genereated by http://www.wowhead.com/data=followers&locale=0&7w19342
             //amount1:
             //amount2:
             //amount3:
             //amount4:
             //category:
             //counters:
             //description:
             //followerclass:
             //hours:
             //icon:
             //id:
             //missionparty:
             //name:
             //race:
             //side:
             //trait:
             //type:
            var b = g_garrison_abilities[e.abilities[c]];   // Information object of current ability
             // Loop all of the "type" array from the garrison ability
            var g = b.type.length;
             // none of the abilities appear to have type 18
            for (var k = 0; k < g; ++k) {
               if (b.type[k] == 18) {
                  if (e.level > f) {
                     f = e.level
                  }
                  if (e.avgilvl > a) {
                     a = e.avgilvl
                  }
               }
            }
         }
      }
   }
   return {
      level: f,
      itemlevel: a
   }
}

//====================================================================
GetFollowerBias: function(d, c) {
    //d = follower level
    //c = follower item level
   var a = (d - this.mission.level) * $WH.fround(1 / 3);
    //this.maxLevel = 100
   if (this.mission.level == this.maxLevel && this.mission.itemlevel > 0) {
      a += (c - this.mission.itemlevel) * $WH.fround(1 / 15)
   }
   var b = -1;
   if (a < -1 || (b = 1, a > 1)) {
      a = b
   }
   return a
}

//====================================================================
CalcChance: function(b, a, c) {
    // ability.amount2 and ability.amount3 come from g_garrison_abilities
    // b = ability.amount2
    // a = ability.amount3
    // c = bias
   var d;
   if (c >= 0) {
      d = (a - b) * c + b
   } else {
      d = (c + 1) * b
   }
   return d
}

//==================================================================
GetMissionTimes: function(g) {
   var e = this.mission.missiontime;
   var h = this.mission.traveltime;
   for (var c = 0; c < g.length; ++c) {
      var d = g[c];
      for (var b = 0; b < d.abilities.length; ++b) {
         var a = g_garrison_abilities[d.abilities[b]];
         var f = a.type.length;
         for (var k = 0; k < f; ++k) {
            if (a.type[k] == 3) {
               h *= a.amount4[k]
            }
            if (a.type[k] == 17) {
               e *= a.amount4[k]
            }
         }
      }
   }
   return {
      missiontime: Math.floor(e),
      traveltime: Math.floor(h)
   }
}

//===========================================================
CheckEffectRace: function(c, a, d) {
   if (this.mission.followers > 0) {
      for (var e = 0; e < c.length; ++e) {
         if (e == d) {
            continue
         }
         var f = c[e];
         var b = g_garrison_followers && g_garrison_followers[f.follower] && g_garrison_followers[f.follower][this.side] ? g_garrison_followers[f.follower][this.side] : null;
         if (b == null) {
            continue
         }
         if (b.race == a) {
            return true
         }
      }
   }
   return false
}

//=================================================================
ThreatCounterIsAlreadyRegistered: function(a, b) {
    // a = index for follower objects
    // b = index for current follower ability
    // c = index for current Type of this ability
    return this.registeredThreatCounters.hasOwnProperty(a) && this.registeredThreatCounters[a].hasOwnProperty(b) && this.registeredThreatCounters[a][b]
}

//====================================================================
RegisterThreatCounter: function(a, b, c) {
    if (!this.registeredThreatCounters.hasOwnProperty(a)) {
        this.registeredThreatCounters[a] = {}
    }
    if (!this.registeredThreatCounters[a].hasOwnProperty(b)) {
        this.registeredThreatCounters[a][b] = {}
    }
    this.registeredThreatCounters[a][b][c] = true
},