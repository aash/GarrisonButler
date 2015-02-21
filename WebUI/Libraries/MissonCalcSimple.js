CalculateSuccessChance: function(c) {
   var n = (this.DEBUG && !c) ? true : false;
   if (n) {
      console.log("----- Start ComputeSuccessChance -----")
   }
   var s = [];
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
   var d = [];
   if (this.mission && this.mission.encounters) {
      for (w in this.mission.encounters) {
         if (this.mission.encounters[w].mechanics) {
            for (var v in this.mission.encounters[w].mechanics) {
               d.push(this.mission.encounters[w].mechanics[v])
            }
         }
      }
   }
   var o = this.GetMentorInfo(s);
   var r;
   for (w = 0; w < s.length; ++w) {
      r = s[w];
      if (o.level > r.level) {
         if (n) {
            console.log("Mentored follower %d from level %d to level %d.", r.follower, r.level, o.level)
         }
         r.level = o.level
      }
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
   var D = this.mission.followers * 100;
   var B = D;
   if (d.length > 0) {
      for (w = 0; w < d.length; ++w) {
         var C = d[w];
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
   var l = 0;
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
   this.registeredThreatCounters = {};
   if (d.length > 0) {
      do {
         C = d[A];
         if (!C.category || C.category == 2) {
            var m = C.amount;
            if (this.mission.followers > 0) {
               for (w = 0; w < s.length; ++w) {
                  r = s[w];
                  for (v = 0; v < r.abilities.length; ++v) {
                     var F = g_garrison_abilities[r.abilities[v]];
                     var e = F.type.length;
                     for (var E = 0; E < e; ++E) {
                        if (C.type == F.counters[E] && !(F.amount1[E] & 1) && m > 0 && !this.ThreatCounterIsAlreadyRegistered(w, v, E)) {
                           var q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                           var a = C.amount;
                           if (q <= a) {
                              a = q
                           }
                           this.RegisterThreatCounter(w, v, E);
                           m = m - a
                        }
                     }
                  }
               }
            }
            if (m < 0) {
               m = 0
            }
            f = $WH.fround((C.amount - m) * b);
            l += f;
            if (n) {
               console.log("Added %.2f to success due to followers countering boss mechanic %d.", f, C.id)
            }
         }++A
      } while (A < d.length)
   }
   for (A = 0; A < d.length; ++A) {
      C = d[A];
      if (C.category == 1) {
         if (this.mission.followers > 0) {
            for (w = 0; w < s.length; ++w) {
               r = s[w];
               for (v = 0; v < r.abilities.length; ++v) {
                  F = g_garrison_abilities[r.abilities[v]];
                  e = F.type.length;
                  for (E = 0; E < e; ++E) {
                     if (C.type == F.counters[E]) {
                        q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                        q *= b;
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
   }
   if (this.mission.followers > 0) {
      for (w = 0; w < s.length; ++w) {
         r = s[w];
         for (v = 0; v < r.abilities.length; ++v) {
            F = g_garrison_abilities[r.abilities[v]];
            e = F.type.length;
            for (E = 0; E < e; ++E) {
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
GetMentorInfo: function(h) {
   var f = 0;
   var a = 0;
   if (this.mission.followers > 0) {
      for (var d = 0; d < h.length; ++d) {
         var e = h[d];
         for (var c = 0; c < e.abilities.length; ++c) {
            //g_garrison_abilities genereated by http://www.wowhead.com/data=followers&locale=0&7w19342
            var b = g_garrison_abilities[e.abilities[c]];
            var g = b.type.length;
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
   var a = (d - this.mission.level) * $WH.fround(1 / 3);
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